using System;
using System.Collections.Immutable;
using SalmonEgg.Domain.Models.Conversation;
using SalmonEgg.Presentation.Core.Mvux.Chat;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public interface IChatStateProjector
{
    ChatUiProjection Apply(
        ChatState storeState,
        ChatConnectionState connectionState,
        string? currentConversationId,
        ConversationRemoteBindingState? binding);
}

public sealed class ChatStateProjector : IChatStateProjector
{
    public ChatUiProjection Apply(
        ChatState storeState,
        ChatConnectionState connectionState,
        string? currentConversationId,
        ConversationRemoteBindingState? binding)
    {
        ArgumentNullException.ThrowIfNull(storeState);
        var selectedProfileId = connectionState.SelectedProfileId;
        var isConnecting = connectionState.Phase == ConnectionPhase.Connecting;
        var isConnected = connectionState.Phase == ConnectionPhase.Connected;
        var isInitializing = connectionState.Phase == ConnectionPhase.Connecting;
        var connectionStatus = connectionState.Phase == ConnectionPhase.Connected ? "Connected" : "Disconnected";
        var connectionError = connectionState.Error;
        var isAuthenticationRequired = connectionState.IsAuthenticationRequired;
        var authenticationHintMessage = connectionState.AuthenticationHintMessage;

        return new ChatUiProjection(
            SelectedConversationId: currentConversationId,
            SelectedProfileId: selectedProfileId,
            RemoteSessionId: binding?.RemoteSessionId,
            IsSessionActive: !string.IsNullOrWhiteSpace(currentConversationId),
            IsPromptInFlight: storeState.IsPromptInFlight,
            IsThinking: storeState.IsThinking,
            IsConnecting: isConnecting,
            IsConnected: isConnected,
            IsInitializing: isInitializing,
            ConnectionStatus: connectionStatus,
            ConnectionError: connectionError,
            IsAuthenticationRequired: isAuthenticationRequired,
            AuthenticationHintMessage: authenticationHintMessage,
            ConnectionGeneration: connectionState.Generation,
            AgentName: storeState.AgentName,
            AgentVersion: storeState.AgentVersion,
            CurrentPrompt: storeState.DraftText ?? string.Empty,
            Transcript: storeState.Transcript ?? ImmutableList<ConversationMessageSnapshot>.Empty,
            ShowPlanPanel: storeState.ShowPlanPanel,
            PlanTitle: storeState.PlanTitle,
            PlanEntries: storeState.PlanEntries ?? ImmutableList<ConversationPlanEntrySnapshot>.Empty);
    }
}

public sealed record ChatUiProjection(
    string? SelectedConversationId,
    string? SelectedProfileId,
    string? RemoteSessionId,
    bool IsSessionActive,
    bool IsPromptInFlight,
    bool IsThinking,
    bool IsConnecting,
    bool IsConnected,
    bool IsInitializing,
    string ConnectionStatus,
    string? ConnectionError,
    bool IsAuthenticationRequired,
    string? AuthenticationHintMessage,
    long ConnectionGeneration,
    string? AgentName,
    string? AgentVersion,
    string CurrentPrompt,
    IImmutableList<ConversationMessageSnapshot> Transcript,
    bool ShowPlanPanel,
    string? PlanTitle,
    IReadOnlyList<ConversationPlanEntrySnapshot> PlanEntries);
