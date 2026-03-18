using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace SalmonEgg.Presentation.Core.Mvux.ShellLayout;

public interface IShellLayoutStore
{
    IFeed<ShellLayoutSnapshot> Snapshot { get; }
    ValueTask Dispatch(ShellLayoutAction action);
}

public sealed class ShellLayoutStore : IShellLayoutStore
{
    private readonly IState<ShellLayoutState> _state;
    private readonly IState<ShellLayoutSnapshot> _snapshotState;
    public IFeed<ShellLayoutSnapshot> Snapshot => _snapshotState;

    public ShellLayoutStore(IState<ShellLayoutState> state, IState<ShellLayoutSnapshot> snapshotState)
    {
        _state = state;
        _snapshotState = snapshotState;
    }

    public async ValueTask Dispatch(ShellLayoutAction action)
    {
        await _state.Update(s =>
        {
            var reduced = ShellLayoutReducer.Reduce(s!, action);
            _snapshotState.Update(_ => reduced.Snapshot, default);
            return reduced.State;
        }, default);
    }
}
