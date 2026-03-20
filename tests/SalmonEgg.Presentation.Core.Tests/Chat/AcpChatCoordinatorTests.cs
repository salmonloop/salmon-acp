using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SalmonEgg.Application.Services.Chat;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Models.Content;
using SalmonEgg.Domain.Models.JsonRpc;
using SalmonEgg.Domain.Models.Protocol;
using SalmonEgg.Domain.Models.Session;
using SalmonEgg.Presentation.Core.Services.Chat;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Chat;

public sealed class AcpChatCoordinatorTests
{
    [Fact]
    public async Task ConnectToProfileAsync_MapsProfileToTransportAndInitializesService()
    {
        var transport = new FakeTransportConfiguration();
        var sink = new FakeSink();
        var service = CreateChatService();
        var factory = new Mock<IAcpChatServiceFactory>();
        var logger = new Mock<ILogger<AcpChatCoordinator>>();

        factory
            .Setup(x => x.CreateChatService(TransportType.Stdio, "agent.exe", "--serve", null))
            .Returns(service.Object);

        var sut = new AcpChatCoordinator(factory.Object, logger.Object);
        var profile = new ServerConfiguration
        {
            Id = "profile-1",
            Name = "Local Agent",
            Transport = TransportType.Stdio,
            StdioCommand = "agent.exe",
            StdioArgs = "--serve"
        };

        var result = await sut.ConnectToProfileAsync(profile, transport, sink);

        Assert.Equal(TransportType.Stdio, transport.SelectedTransportType);
        Assert.Equal("agent.exe", transport.StdioCommand);
        Assert.Equal("--serve", transport.StdioArgs);
        Assert.Equal(string.Empty, transport.RemoteUrl);
        Assert.Same(service.Object, sink.CurrentChatService);
        Assert.Equal("profile-1", sink.SelectedProfileId);
        Assert.True(sink.IsConnected);
        Assert.True(sink.IsInitialized);
        Assert.Equal("agent", sink.AgentName);
        Assert.Equal("1.0.0", sink.AgentVersion);
        Assert.Same(service.Object, result.ChatService);
        service.Verify(x => x.InitializeAsync(It.IsAny<InitializeParams>()), Times.Once);
    }

    [Fact]
    public async Task ApplyTransportConfigurationAsync_DisconnectsExistingServiceBeforeReplacingIt()
    {
        var transport = new FakeTransportConfiguration
        {
            SelectedTransportType = TransportType.WebSocket,
            RemoteUrl = "wss://agent.test"
        };
        var oldService = CreateChatService();
        var newService = CreateChatService();
        var sink = new FakeSink
        {
            CurrentChatService = oldService.Object
        };
        var factory = new Mock<IAcpChatServiceFactory>();
        var logger = new Mock<ILogger<AcpChatCoordinator>>();

        factory
            .Setup(x => x.CreateChatService(TransportType.WebSocket, null, null, "wss://agent.test"))
            .Returns(newService.Object);

        var sut = new AcpChatCoordinator(factory.Object, logger.Object);

        var result = await sut.ApplyTransportConfigurationAsync(transport, sink, preserveConversation: true);

        oldService.Verify(x => x.DisconnectAsync(), Times.Once);
        Assert.Same(newService.Object, sink.CurrentChatService);
        Assert.Same(newService.Object, result.ChatService);
    }

    [Fact]
    public async Task EnsureRemoteSessionAsync_CreatesAndBindsRemoteSessionWhenMissing()
    {
        var service = CreateChatService();
        var sink = new FakeSink
        {
            CurrentChatService = service.Object,
            IsConnected = true,
            IsInitialized = true,
            IsSessionActive = true,
            CurrentSessionId = "local-session-1",
            ActiveSessionCwd = @"C:\repo\demo",
            SelectedProfileId = "profile-1"
        };
        var factory = new Mock<IAcpChatServiceFactory>();
        var logger = new Mock<ILogger<AcpChatCoordinator>>();

        service
            .Setup(x => x.CreateSessionAsync(It.IsAny<SessionNewParams>()))
            .ReturnsAsync(new SessionNewResponse("remote-session-1"));

        var sut = new AcpChatCoordinator(factory.Object, logger.Object);

        var result = await sut.EnsureRemoteSessionAsync(sink, _ => Task.FromResult(true));

        Assert.Equal("remote-session-1", result.RemoteSessionId);
        Assert.Equal("remote-session-1", sink.CurrentRemoteSessionId);
        Assert.Single(sink.BoundRemoteSessions);
        Assert.Equal("profile-1", sink.BoundRemoteSessions[0].ProfileId);
    }

    [Fact]
    public async Task SendPromptAsync_RemoteSessionNotFound_ClearsBindingRecreatesSessionAndRetriesOnce()
    {
        var service = CreateChatService();
        var sink = new FakeSink
        {
            CurrentChatService = service.Object,
            IsConnected = true,
            IsInitialized = true,
            IsSessionActive = true,
            CurrentSessionId = "local-session-1",
            CurrentRemoteSessionId = "remote-stale",
            ActiveSessionCwd = @"C:\repo\demo",
            SelectedProfileId = "profile-1"
        };
        var factory = new Mock<IAcpChatServiceFactory>();
        var logger = new Mock<ILogger<AcpChatCoordinator>>();
        var firstPrompt = true;

        service
            .Setup(x => x.SendPromptAsync(It.IsAny<SessionPromptParams>(), It.IsAny<CancellationToken>()))
            .Returns<SessionPromptParams, CancellationToken>((parameters, _) =>
            {
                if (firstPrompt)
                {
                    firstPrompt = false;
                    Assert.Equal("remote-stale", parameters.SessionId);
                    throw new AcpException(JsonRpcErrorCode.ResourceNotFound, "session not found");
                }

                Assert.Equal("remote-session-2", parameters.SessionId);
                return Task.FromResult(new SessionPromptResponse());
            });

        service
            .Setup(x => x.CreateSessionAsync(It.IsAny<SessionNewParams>()))
            .ReturnsAsync(new SessionNewResponse("remote-session-2"));

        var sut = new AcpChatCoordinator(factory.Object, logger.Object);

        var result = await sut.SendPromptAsync("hello", sink, _ => Task.FromResult(true));

        Assert.True(result.RetriedAfterSessionRecovery);
        Assert.Equal("remote-session-2", result.RemoteSessionId);
        Assert.Equal(1, sink.ClearedRemoteSessionBindings);
        Assert.Equal("remote-session-2", sink.CurrentRemoteSessionId);
        service.Verify(x => x.SendPromptAsync(It.IsAny<SessionPromptParams>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CancelPromptAsync_UsesCurrentRemoteSessionFromState()
    {
        var service = CreateChatService();
        var sink = new FakeSink
        {
            CurrentChatService = service.Object,
            CurrentRemoteSessionId = "remote-session-9"
        };
        var factory = new Mock<IAcpChatServiceFactory>();
        var logger = new Mock<ILogger<AcpChatCoordinator>>();
        SessionCancelParams? captured = null;

        service
            .Setup(x => x.CancelSessionAsync(It.IsAny<SessionCancelParams>()))
            .Callback<SessionCancelParams>(parameters => captured = parameters)
            .ReturnsAsync(new SessionCancelResponse(true));

        var sut = new AcpChatCoordinator(factory.Object, logger.Object);

        await sut.CancelPromptAsync(sink, "User cancelled");

        Assert.NotNull(captured);
        Assert.Equal("remote-session-9", captured!.SessionId);
        Assert.Equal("User cancelled", captured.Reason);
    }

    [Fact]
    public async Task DisconnectAsync_DisconnectsServiceAndClearsCoordinatorState()
    {
        var service = CreateChatService();
        var sink = new FakeSink
        {
            CurrentChatService = service.Object,
            IsConnected = true,
            IsInitialized = true,
            CurrentRemoteSessionId = "remote-session-3"
        };
        var factory = new Mock<IAcpChatServiceFactory>();
        var logger = new Mock<ILogger<AcpChatCoordinator>>();
        var sut = new AcpChatCoordinator(factory.Object, logger.Object);

        await sut.DisconnectAsync(sink);

        service.Verify(x => x.DisconnectAsync(), Times.Once);
        Assert.Null(sink.CurrentChatService);
        Assert.False(sink.IsConnected);
        Assert.False(sink.IsInitialized);
        Assert.Null(sink.CurrentRemoteSessionId);
        Assert.Equal(1, sink.ClearedRemoteSessionBindings);
    }

    private static Mock<IChatService> CreateChatService()
    {
        var service = new Mock<IChatService>();
        service.SetupGet(x => x.IsConnected).Returns(true);
        service.SetupGet(x => x.IsInitialized).Returns(true);
        service.SetupGet(x => x.SessionHistory).Returns(Array.Empty<SessionUpdateEntry>());
        service.Setup(x => x.DisconnectAsync()).ReturnsAsync(true);
        service.Setup(x => x.InitializeAsync(It.IsAny<InitializeParams>()))
            .ReturnsAsync(new InitializeResponse(1, new AgentInfo("agent", "1.0.0"), new AgentCapabilities()));
        return service;
    }

    private sealed class FakeTransportConfiguration : IAcpTransportConfiguration
    {
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public TransportType SelectedTransportType { get; set; }
        public string StdioCommand { get; set; } = string.Empty;
        public string StdioArgs { get; set; } = string.Empty;
        public string RemoteUrl { get; set; } = string.Empty;
        public bool IsValid { get; set; } = true;
        public string? ValidationError { get; set; }

        public (bool IsValid, string? ErrorMessage) Validate() => (IsValid, ValidationError);
    }

    private sealed class FakeSink : IAcpChatCoordinatorSink
    {
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public IChatService? CurrentChatService { get; set; }
        public bool IsConnected { get; set; }
        public bool IsInitialized { get; set; }
        public bool IsConnecting { get; private set; }
        public bool IsInitializing { get; private set; }
        public bool IsSessionActive { get; set; }
        public bool IsAuthenticationRequired { get; private set; }
        public string? ConnectionErrorMessage { get; private set; }
        public string? AuthenticationHintMessage { get; private set; }
        public string? AgentName { get; private set; }
        public string? AgentVersion { get; private set; }
        public string? CurrentSessionId { get; set; }
        public string? CurrentRemoteSessionId { get; set; }
        public string? SelectedProfileId { get; set; }
        public string ActiveSessionCwd { get; set; } = string.Empty;
        public int ClearedRemoteSessionBindings { get; private set; }
        public List<(string RemoteSessionId, string? ProfileId, bool PreserveConversation)> BoundRemoteSessions { get; } = new();

        public void SelectProfile(ServerConfiguration profile)
        {
            SelectedProfileId = profile.Id;
        }

        public void ReplaceChatService(IChatService? chatService)
        {
            CurrentChatService = chatService;
        }

        public void UpdateConnectionState(bool isConnecting, bool isConnected, bool isInitialized, string? errorMessage)
        {
            IsConnecting = isConnecting;
            IsConnected = isConnected;
            IsInitialized = isInitialized;
            ConnectionErrorMessage = errorMessage;
        }

        public void UpdateInitializationState(bool isInitializing)
        {
            IsInitializing = isInitializing;
        }

        public void UpdateAuthenticationState(bool isRequired, string? hintMessage)
        {
            IsAuthenticationRequired = isRequired;
            AuthenticationHintMessage = hintMessage;
        }

        public void UpdateAgentIdentity(string? agentName, string? agentVersion)
        {
            AgentName = agentName;
            AgentVersion = agentVersion;
        }

        public void BindRemoteSession(string remoteSessionId, string? profileId, SessionNewResponse response, bool preserveConversation)
        {
            CurrentRemoteSessionId = remoteSessionId;
            BoundRemoteSessions.Add((remoteSessionId, profileId, preserveConversation));
        }

        public void ClearRemoteSessionBinding()
        {
            ClearedRemoteSessionBindings++;
            CurrentRemoteSessionId = null;
        }

        public string GetActiveSessionCwdOrDefault() => ActiveSessionCwd;
    }
}
