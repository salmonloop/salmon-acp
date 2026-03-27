using System;
using System.Linq;
using System.Threading.Tasks;
using SalmonEgg.Presentation.Core.Mvux.Chat;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public sealed class BindingCoordinator : IConversationBindingCommands
{
    private readonly ChatConversationWorkspace _workspace;
    private readonly IChatStore _chatStore;

    public BindingCoordinator(ChatConversationWorkspace workspace, IChatStore chatStore)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _chatStore = chatStore ?? throw new ArgumentNullException(nameof(chatStore));
    }

    public async ValueTask<BindingUpdateResult> UpdateBindingAsync(string conversationId, string? remoteSessionId, string? boundProfileId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return BindingUpdateResult.Error("ConversationIdMissing");
        }

        try
        {
            var conversationExists = _workspace
                .GetKnownConversationIds()
                .Contains(conversationId, StringComparer.Ordinal);

            var binding = new ConversationBindingSlice(conversationId, remoteSessionId, boundProfileId);
            await _chatStore.Dispatch(new SetBindingSliceAction(binding)).ConfigureAwait(false);
            var projectionVisible = await WaitForProjectedBindingAsync(binding).ConfigureAwait(false);
            if (!projectionVisible)
            {
                return BindingUpdateResult.Error("BindingProjectionTimeout");
            }

            if (conversationExists)
            {
                _workspace.UpdateRemoteBinding(conversationId, remoteSessionId, boundProfileId);
                _workspace.ScheduleSave();
            }
            return BindingUpdateResult.Success();
        }
        catch (Exception ex)
        {
            return BindingUpdateResult.Error(ex.Message);
        }
    }

    private async Task<bool> WaitForProjectedBindingAsync(
        ConversationBindingSlice expectedBinding,
        int timeoutMilliseconds = 500,
        int pollDelayMilliseconds = 10)
    {
        var timeoutAt = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (DateTime.UtcNow < timeoutAt)
        {
            var state = await _chatStore.State;
            var currentState = state ?? ChatState.Empty;
            var actualBinding = currentState.ResolveBinding(expectedBinding.ConversationId);
            if (actualBinding == expectedBinding)
            {
                return true;
            }

            await Task.Delay(pollDelayMilliseconds).ConfigureAwait(false);
        }

        var finalStateValue = await _chatStore.State;
        var finalState = finalStateValue ?? ChatState.Empty;
        return finalState.ResolveBinding(expectedBinding.ConversationId) == expectedBinding;
    }
}
