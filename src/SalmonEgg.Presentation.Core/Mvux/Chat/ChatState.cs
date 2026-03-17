using System.Collections.Immutable;

namespace SalmonEgg.Presentation.Core.Mvux.Chat;

public record ChatState(
    string? SelectedConversationId = null,
    string? SelectedAcpProfileId = null,
    bool IsPromptInFlight = false,
    bool IsThinking = false,
    string ConnectionStatus = "Disconnected",
    string? ConnectionError = null,
    IImmutableList<ChatMessage>? Messages = null,
    string DraftText = "")
{
    public static ChatState Empty { get; } = new();
}
