using System;
using System.Threading.Tasks;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;
using Uno.Extensions.Reactive;

namespace SalmonEgg.Presentation.Services;

public sealed class ShellLayoutNavigationStateAdapter : INavigationPaneState, IDisposable
{
    private readonly IDisposable? _subscription;
    private readonly IState<ShellLayoutSnapshot>? _snapshotState;
    private bool _isPaneOpen;
    public bool IsPaneOpen => _isPaneOpen;
    public event EventHandler? PaneStateChanged;

    public ShellLayoutNavigationStateAdapter(IShellLayoutStore store)
    {
        _snapshotState = State.FromFeed(this, store.Snapshot);
        // Using the ForEach overload that worked in ShellLayoutViewModel
        _snapshotState.ForEach(async (snapshot, ct) =>
        {
            if (snapshot is null) return;
            if (_isPaneOpen == snapshot.IsNavPaneOpen) return;
            _isPaneOpen = snapshot.IsNavPaneOpen;
            PaneStateChanged?.Invoke(this, EventArgs.Empty);
        }, out _subscription);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        if (_snapshotState is IAsyncDisposable asyncDisposable)
        {
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
