using System;
using System.Threading;
using System.Threading.Tasks;
using SalmonEgg.Presentation.Core.Mvux.Chat;
using Uno.Extensions.Reactive;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public interface IChatConnectionStore
{
    IState<ChatConnectionState> State { get; }

    ValueTask Dispatch(ChatConnectionAction action);

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
