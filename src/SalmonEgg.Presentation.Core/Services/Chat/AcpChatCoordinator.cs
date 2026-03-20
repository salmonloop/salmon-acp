using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SalmonEgg.Application.Services.Chat;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Models.Content;
using SalmonEgg.Domain.Models.JsonRpc;
using SalmonEgg.Domain.Models.Mcp;
using SalmonEgg.Domain.Models.Protocol;

namespace SalmonEgg.Presentation.Core.Services.Chat;

/// <summary>
/// Minimal ACP service lifecycle coordinator.
/// This slice extracts transport/profile/service seams so ChatViewModel can delegate incrementally.
/// </summary>
public sealed class AcpChatCoordinator : IAcpConnectionCommands
{
    private readonly IAcpChatServiceFactory _chatServiceFactory;
    private readonly ILogger<AcpChatCoordinator> _logger;

    public AcpChatCoordinator(
        IAcpChatServiceFactory chatServiceFactory,
        ILogger<AcpChatCoordinator> logger)
    {
        _chatServiceFactory = chatServiceFactory ?? throw new ArgumentNullException(nameof(chatServiceFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AcpTransportApplyResult> ConnectToProfileAsync(
        ServerConfiguration profile,
        IAcpTransportConfiguration transportConfiguration,
        IAcpChatCoordinatorSink sink,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(transportConfiguration);
        ArgumentNullException.ThrowIfNull(sink);

        sink.SelectProfile(profile);
        ApplyProfileToTransportConfiguration(profile, transportConfiguration);

        var preserveConversation = sink.IsSessionActive && !string.IsNullOrWhiteSpace(sink.CurrentSessionId);
        return await ApplyTransportConfigurationAsync(
            transportConfiguration,
            sink,
            preserveConversation,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<AcpTransportApplyResult> ApplyTransportConfigurationAsync(
        IAcpTransportConfiguration transportConfiguration,
        IAcpChatCoordinatorSink sink,
        bool preserveConversation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transportConfiguration);
        ArgumentNullException.ThrowIfNull(sink);
        cancellationToken.ThrowIfCancellationRequested();

        var (isValid, errorMessage) = transportConfiguration.Validate();
        if (!isValid)
        {
            sink.UpdateConnectionState(isConnecting: false, isConnected: false, isInitialized: false, errorMessage);
            throw new InvalidOperationException(errorMessage ?? "Invalid ACP transport configuration.");
        }

        sink.UpdateConnectionState(isConnecting: true, isConnected: sink.IsConnected, isInitialized: sink.IsInitialized, errorMessage: null);
        sink.UpdateInitializationState(isInitializing: true);

        var previousService = sink.CurrentChatService;
        try
        {
            var newService = _chatServiceFactory.CreateChatService(
                transportConfiguration.SelectedTransportType,
                transportConfiguration.SelectedTransportType == TransportType.Stdio ? transportConfiguration.StdioCommand : null,
                transportConfiguration.SelectedTransportType == TransportType.Stdio ? transportConfiguration.StdioArgs : null,
                transportConfiguration.SelectedTransportType == TransportType.Stdio ? null : transportConfiguration.RemoteUrl);

            if (previousService != null)
            {
                try
                {
                    await previousService.DisconnectAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to disconnect previous ACP service during transport replacement");
                }
            }

            sink.ReplaceChatService(newService);

            var initializeResponse = await newService.InitializeAsync(CreateDefaultInitializeParams()).ConfigureAwait(false);
            sink.UpdateAgentIdentity(initializeResponse.AgentInfo?.Name, initializeResponse.AgentInfo?.Version);
            sink.UpdateConnectionState(
                isConnecting: false,
                isConnected: newService.IsConnected,
                isInitialized: newService.IsInitialized,
                errorMessage: null);
            sink.UpdateAuthenticationState(isRequired: false, hintMessage: null);

            _logger.LogInformation(
                "ACP transport applied. transport={TransportType} preserveConversation={PreserveConversation}",
                transportConfiguration.SelectedTransportType,
                preserveConversation);

            return new AcpTransportApplyResult(newService, initializeResponse);
        }
        catch (Exception ex)
        {
            try
            {
                if (sink.CurrentChatService != null)
                {
                    await sink.CurrentChatService.DisconnectAsync().ConfigureAwait(false);
                }
            }
            catch (Exception disconnectEx)
            {
                _logger.LogDebug(disconnectEx, "Failed to tear down ACP service after initialization error");
            }

            sink.ReplaceChatService(null);
            sink.UpdateAgentIdentity(null, null);
            sink.UpdateConnectionState(isConnecting: false, isConnected: false, isInitialized: false, ex.Message);
            _logger.LogError(ex, "Failed to apply ACP transport configuration");
            throw;
        }
        finally
        {
            sink.UpdateInitializationState(isInitializing: false);
        }
    }

    public async Task<AcpRemoteSessionResult> EnsureRemoteSessionAsync(
        IAcpChatCoordinatorSink sink,
        Func<CancellationToken, Task<bool>> authenticateAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(authenticateAsync);

        var chatService = RequireReadyChatService(sink);
        if (!sink.IsSessionActive || string.IsNullOrWhiteSpace(sink.CurrentSessionId))
        {
            throw new InvalidOperationException("No active local conversation is available for ACP session creation.");
        }

        if (!string.IsNullOrWhiteSpace(sink.CurrentRemoteSessionId))
        {
            return new AcpRemoteSessionResult(
                sink.CurrentRemoteSessionId!,
                new SessionNewResponse(sink.CurrentRemoteSessionId!),
                UsedExistingBinding: true);
        }

        var sessionParams = new SessionNewParams(
            sink.GetActiveSessionCwdOrDefault(),
            new List<McpServer>());

        SessionNewResponse response;
        try
        {
            response = await chatService.CreateSessionAsync(sessionParams).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsAuthenticationRequiredError(ex))
        {
            var authenticated = await authenticateAsync(cancellationToken).ConfigureAwait(false);
            if (!authenticated)
            {
                throw new InvalidOperationException(
                    sink.AuthenticationHintMessage ?? "The agent requires authentication before it can respond.",
                    ex);
            }

            response = await chatService.CreateSessionAsync(sessionParams).ConfigureAwait(false);
        }

        sink.BindRemoteSession(response.SessionId, sink.SelectedProfileId, response, preserveConversation: true);
        return new AcpRemoteSessionResult(response.SessionId, response, UsedExistingBinding: false);
    }

    public async Task<AcpPromptDispatchResult> SendPromptAsync(
        string promptText,
        IAcpChatCoordinatorSink sink,
        Func<CancellationToken, Task<bool>> authenticateAsync,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptText))
        {
            throw new ArgumentException("Prompt text must not be empty.", nameof(promptText));
        }

        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(authenticateAsync);

        var chatService = RequireReadyChatService(sink);

        if (sink.IsAuthenticationRequired)
        {
            var authenticated = await authenticateAsync(cancellationToken).ConfigureAwait(false);
            if (!authenticated)
            {
                throw new InvalidOperationException(
                    sink.AuthenticationHintMessage ?? "The agent requires authentication before it can respond.");
            }
        }

        var remoteSession = !string.IsNullOrWhiteSpace(sink.CurrentRemoteSessionId)
            ? new AcpRemoteSessionResult(
                sink.CurrentRemoteSessionId!,
                new SessionNewResponse(sink.CurrentRemoteSessionId!),
                UsedExistingBinding: true)
            : await EnsureRemoteSessionAsync(sink, authenticateAsync, cancellationToken).ConfigureAwait(false);

        var promptParams = new SessionPromptParams(
            remoteSession.RemoteSessionId,
            new List<ContentBlock> { new TextContentBlock { Text = promptText } });

        try
        {
            var response = await chatService.SendPromptAsync(promptParams, cancellationToken).ConfigureAwait(false);
            return new AcpPromptDispatchResult(promptParams.SessionId, response, RetriedAfterSessionRecovery: false);
        }
        catch (Exception ex) when (IsAuthenticationRequiredError(ex))
        {
            var authenticated = await authenticateAsync(cancellationToken).ConfigureAwait(false);
            if (!authenticated)
            {
                throw new InvalidOperationException(
                    sink.AuthenticationHintMessage ?? "The agent requires authentication before it can respond.",
                    ex);
            }

            var response = await chatService.SendPromptAsync(promptParams, cancellationToken).ConfigureAwait(false);
            return new AcpPromptDispatchResult(promptParams.SessionId, response, RetriedAfterSessionRecovery: false);
        }
        catch (Exception ex) when (IsRemoteSessionNotFound(ex))
        {
            sink.ClearRemoteSessionBinding();
            var recreated = await EnsureRemoteSessionAsync(sink, authenticateAsync, cancellationToken).ConfigureAwait(false);
            promptParams.SessionId = recreated.RemoteSessionId;

            var response = await chatService.SendPromptAsync(promptParams, cancellationToken).ConfigureAwait(false);
            return new AcpPromptDispatchResult(promptParams.SessionId, response, RetriedAfterSessionRecovery: true);
        }
    }

    public async Task CancelPromptAsync(
        IAcpChatCoordinatorSink sink,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sink);

        var chatService = sink.CurrentChatService;
        if (chatService == null || string.IsNullOrWhiteSpace(sink.CurrentRemoteSessionId))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await chatService.CancelSessionAsync(
            new SessionCancelParams(sink.CurrentRemoteSessionId!, reason)).ConfigureAwait(false);
    }

    public async Task DisconnectAsync(
        IAcpChatCoordinatorSink sink,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sink);
        cancellationToken.ThrowIfCancellationRequested();

        var chatService = sink.CurrentChatService;
        if (chatService != null)
        {
            await chatService.DisconnectAsync().ConfigureAwait(false);
        }

        sink.ReplaceChatService(null);
        sink.ClearRemoteSessionBinding();
        sink.UpdateAuthenticationState(isRequired: false, hintMessage: null);
        sink.UpdateAgentIdentity(null, null);
        sink.UpdateInitializationState(isInitializing: false);
        sink.UpdateConnectionState(isConnecting: false, isConnected: false, isInitialized: false, errorMessage: null);
    }

    private static void ApplyProfileToTransportConfiguration(
        ServerConfiguration profile,
        IAcpTransportConfiguration transportConfiguration)
    {
        transportConfiguration.SelectedTransportType = profile.Transport;

        if (profile.Transport == TransportType.Stdio)
        {
            transportConfiguration.StdioCommand = profile.StdioCommand ?? string.Empty;
            transportConfiguration.StdioArgs = profile.StdioArgs ?? string.Empty;
            transportConfiguration.RemoteUrl = string.Empty;
            return;
        }

        transportConfiguration.RemoteUrl = profile.ServerUrl ?? string.Empty;
        transportConfiguration.StdioCommand = string.Empty;
        transportConfiguration.StdioArgs = string.Empty;
    }

    private static IChatService RequireReadyChatService(IAcpChatCoordinatorSink sink)
    {
        if (sink.CurrentChatService is not { IsConnected: true, IsInitialized: true } chatService)
        {
            throw new InvalidOperationException("ACP chat service is not connected and initialized.");
        }

        return chatService;
    }

    private static bool IsAuthenticationRequiredError(Exception ex) =>
        ex is AcpException acp && acp.ErrorCode == JsonRpcErrorCode.AuthenticationRequired;

    private static bool IsRemoteSessionNotFound(Exception ex) =>
        ex is AcpException acp
        && (acp.ErrorCode == JsonRpcErrorCode.ResourceNotFound
            || (acp.Message.Contains("Session", StringComparison.OrdinalIgnoreCase)
                && acp.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)));

    private static InitializeParams CreateDefaultInitializeParams()
        => new()
        {
            ProtocolVersion = 1,
            ClientInfo = new ClientInfo
            {
                Name = "SalmonEgg",
                Title = "Uno Acp Client",
                Version = "1.0.0"
            },
            ClientCapabilities = new ClientCapabilities
            {
                Fs = new FsCapability
                {
                    ReadTextFile = true,
                    WriteTextFile = true
                }
            }
        };
}
