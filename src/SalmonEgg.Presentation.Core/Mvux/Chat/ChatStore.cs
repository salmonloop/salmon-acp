using System;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace SalmonEgg.Presentation.Core.Mvux.Chat;

/// <summary>
/// Defines the Single Source of Truth for the Chat feature.
/// </summary>
public interface IChatStore
{
    /// <summary>
    /// Gets the current state of the chat.
    /// </summary>
    IState<ChatState> State { get; }

    /// <summary>
    /// Dispatches an action to update the state via the reducer.
    /// </summary>
    /// <param name="action">The action to dispatch.</param>
    ValueTask Dispatch(ChatAction action);
}

/// <summary>
/// Implementation of the Chat Store using Uno.Extensions.Reactive.
/// </summary>
public sealed class ChatStore : IChatStore
{
    public IState<ChatState> State { get; }

    public ChatStore(IState<ChatState> state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public async ValueTask Dispatch(ChatAction action)
    {
        await State.Update(s => ChatReducer.Reduce(s!, action), default);
    }
}
