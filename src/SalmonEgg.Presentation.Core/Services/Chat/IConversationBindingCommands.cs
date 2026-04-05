namespace SalmonEgg.Presentation.Core.Services.Chat;

public interface IConversationBindingCommands
{
    ValueTask<BindingUpdateResult> UpdateBindingAsync(string conversationId, string? remoteSessionId, string? boundProfileId);

    ValueTask<BindingUpdateResult> ClearBindingAsync(string conversationId)
        => UpdateBindingAsync(conversationId, remoteSessionId: null, boundProfileId: null);
}
