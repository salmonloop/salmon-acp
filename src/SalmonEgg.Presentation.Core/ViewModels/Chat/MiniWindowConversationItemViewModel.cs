using System;

namespace SalmonEgg.Presentation.ViewModels.Chat;

public sealed class MiniWindowConversationItemViewModel
{
    public MiniWindowConversationItemViewModel(string conversationId, string displayName)
    {
        ConversationId = conversationId ?? throw new ArgumentNullException(nameof(conversationId));
        DisplayName = displayName ?? string.Empty;
    }

    public string ConversationId { get; }

    public string DisplayName { get; }
}

