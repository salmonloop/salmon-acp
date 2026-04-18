using System;

namespace SalmonEgg.Presentation.ViewModels.Chat;

public sealed class MiniWindowConversationItemViewModel
{
    public MiniWindowConversationItemViewModel(string conversationId, string displayName, string compactDisplayName)
    {
        ConversationId = conversationId ?? throw new ArgumentNullException(nameof(conversationId));
        DisplayName = displayName ?? string.Empty;
        CompactDisplayName = compactDisplayName ?? string.Empty;
    }

    public string ConversationId { get; }

    public string DisplayName { get; }

    public string CompactDisplayName { get; }
}
