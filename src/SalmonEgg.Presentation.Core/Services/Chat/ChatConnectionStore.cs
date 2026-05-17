using System;
using System.Threading;
using System.Threading.Tasks;
using SalmonEgg.Presentation.Core.Mvux.Chat;
using Uno.Extensions.Reactive;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public interface IChatConnectionStore
{
    /// <summary>
    /// Reactive projection for UI subscribers. It can observe committed state asynchronously.
    /// </summary>
    IState<ChatConnectionState> State { get; }

    /// <summary>
    /// Applies an action to the authoritative connection state.
    /// </summary>
    ValueTask Dispatch(ChatConnectionAction action);

    /// <summary>
    /// Returns the latest authoritative state committed by this store, including state that
    /// downstream reactive projections may not have observed yet.
    /// </summary>
    ValueTask<ChatConnectionState> GetCurrentStateAsync();
}

public sealed class ChatConnectionStore : IChatConnectionStore
{
    private readonly SemaphoreSlim _dispatchGate = new(1, 1);
    private ChatConnectionState? _cachedState;

    public IState<ChatConnectionState> State { get; }

    public ChatConnectionStore(IState<ChatConnectionState> state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public async ValueTask Dispatch(ChatConnectionAction action)
    {
        await _dispatchGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var currentState = _cachedState ?? await State ?? ChatConnectionState.Empty;
            var updatedState = ChatConnectionReducer.Reduce(currentState, action);
            _cachedState = updatedState;

            await State.Update(_ => updatedState, default).ConfigureAwait(false);
        }
        finally
        {
            _dispatchGate.Release();
        }
    }

    public async ValueTask<ChatConnectionState> GetCurrentStateAsync()
        => _cachedState ?? await State ?? ChatConnectionState.Empty;
}
