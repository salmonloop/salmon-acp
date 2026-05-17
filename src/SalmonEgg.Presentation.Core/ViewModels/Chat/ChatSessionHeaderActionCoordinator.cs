using System;
using SalmonEgg.Presentation.Core.Services.Chat;

namespace SalmonEgg.Presentation.ViewModels.Chat;

public sealed class ChatSessionHeaderActionCoordinator
{
    public bool TryApplyProjectAffinityOverride(
        ChatConversationWorkspace conversationWorkspace,
        string? currentSessionId,
        string? selectedProjectId)
    {
        ArgumentNullException.ThrowIfNull(conversationWorkspace);

        if (string.IsNullOrWhiteSpace(currentSessionId)
            || string.IsNullOrWhiteSpace(selectedProjectId))
        {
            return false;
        }

        conversationWorkspace.UpdateProjectAffinityOverride(currentSessionId, selectedProjectId);
        return true;
    }

    public bool TryClearProjectAffinityOverride(
        ChatConversationWorkspace conversationWorkspace,
        string? currentSessionId)
    {
        ArgumentNullException.ThrowIfNull(conversationWorkspace);

        if (string.IsNullOrWhiteSpace(currentSessionId))
        {
            return false;
        }

        conversationWorkspace.UpdateProjectAffinityOverride(currentSessionId, null);
        return true;
    }
}
