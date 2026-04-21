using System;

namespace SalmonEgg.Presentation.ViewModels.Chat;

public sealed class MiniWindowConversationItemViewModel
{
    public MiniWindowConversationItemViewModel(
        string conversationId,
        string displayName,
        string compactDisplayName,
        bool hasUnreadAttention = false)
    {
        ConversationId = conversationId ?? throw new ArgumentNullException(nameof(conversationId));
        DisplayName = displayName ?? string.Empty;
        CompactDisplayName = compactDisplayName ?? string.Empty;
        HasUnreadAttention = hasUnreadAttention;
    }

    public string ConversationId { get; }

    public string DisplayName { get; }

    public string CompactDisplayName { get; }

    public bool HasUnreadAttention { get; }

    public string AutomationId => $"MiniChat.SessionItem.{ConversationId}";
}
