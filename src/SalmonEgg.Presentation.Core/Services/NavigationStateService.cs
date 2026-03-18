using System;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;
using Uno.Extensions.Reactive;

namespace SalmonEgg.Presentation.Services;

public sealed class NavigationStateService : INavigationStateService, IDisposable
{
    private readonly IDisposable? _subscription;
    private readonly IState<ShellLayoutSnapshot>? _snapshotState;
    private bool _isPaneOpen = true;

    public bool IsPaneOpen => _isPaneOpen;

    public event EventHandler? PaneStateChanged;

    public NavigationStateService(IShellLayoutStore store)
    {
        _snapshotState = State.FromFeed(this, store.Snapshot);
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
