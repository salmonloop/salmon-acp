using System;
using System.Threading;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;
using SalmonEgg.Presentation.Core.Services;
using Uno.Extensions.Reactive;

namespace SalmonEgg.Presentation.Services;

public sealed class NavigationStateService : INavigationStateService, IDisposable
{
    private readonly IDisposable? _subscription;
    private readonly IState<ShellLayoutSnapshot>? _snapshotState;
    private readonly IUiDispatcher _uiDispatcher;
    private bool _isPaneOpen;

    public bool IsPaneOpen => _isPaneOpen;

    public event EventHandler? PaneStateChanged;

    public NavigationStateService(IShellLayoutStore store, IUiDispatcher uiDispatcher)
    {
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _isPaneOpen = store.CurrentSnapshot.IsNavPaneOpen;
        _snapshotState = State.FromFeed(this, store.Snapshot);
        _snapshotState.ForEach(async (snapshot, ct) =>
        {
            if (snapshot is null) return;
            if (_isPaneOpen == snapshot.IsNavPaneOpen) return;

            _isPaneOpen = snapshot.IsNavPaneOpen;
            RaisePaneStateChanged();
        }, out _subscription);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    private void RaisePaneStateChanged()
    {
        if (_uiDispatcher.HasThreadAccess)
        {
            PaneStateChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        _uiDispatcher.Enqueue(() => PaneStateChanged?.Invoke(this, EventArgs.Empty));
    }
}
