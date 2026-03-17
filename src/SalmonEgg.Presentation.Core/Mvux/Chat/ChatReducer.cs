using System;
using System.Collections.Immutable;
using System.Linq;

namespace SalmonEgg.Presentation.Core.Mvux.Chat;

public static class ChatReducer
{
    public static ChatState Reduce(ChatState state, ChatAction action)
    {
        return action switch
        {
            SelectConversationAction selectConversation => state with { SelectedConversationId = selectConversation.ConversationId },
            SetPromptInFlightAction setPromptInFlight => state with { IsPromptInFlight = setPromptInFlight.IsInFlight },
            SetIsThinkingAction setIsThinking => state with { IsThinking = setIsThinking.IsThinking },
            AddMessageAction addMessage => state with { Messages = (state.Messages ?? ImmutableList<ChatMessage>.Empty).Add(addMessage.Message) },
            UpdateMessageAction updateMessage => state with
            {
                Messages = state.Messages is { } messages && messages.FirstOrDefault(m => m.Id == updateMessage.Message.Id) is { } existing
                    ? messages.Replace(existing, updateMessage.Message)
                    : state.Messages
            },
            AppendTextDeltaAction appendDelta => state with
            {
                Messages = (state.Messages ?? ImmutableList<ChatMessage>.Empty) switch
                {
                    { Count: > 0 } msgs when !msgs[^1].IsOutgoing => msgs.SetItem(msgs.Count - 1, msgs[^1].MergeDelta(appendDelta.Delta)),
                    var msgs => msgs.Add(new ChatMessage(Guid.NewGuid().ToString(), DateTimeOffset.Now, false).MergeDelta(appendDelta.Delta))
                }
            },
            UpdateConnectionStatusAction updateStatus => state with
            {
                ConnectionStatus = updateStatus.IsConnected ? "Connected" : "Disconnected",
                ConnectionError = updateStatus.ErrorMessage
            },
            _ => state
        };
    }
}
