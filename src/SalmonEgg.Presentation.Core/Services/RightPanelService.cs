using System;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;
using Uno.Extensions.Reactive;

namespace SalmonEgg.Presentation.Services;

public sealed class RightPanelService : IRightPanelService, IDisposable
{
    private readonly IDisposable? _subscription;
    private readonly IState<ShellLayoutSnapshot>? _snapshotState;
    private RightPanelMode _currentMode = RightPanelMode.None;
    private double _panelWidth = 320;

    public RightPanelMode CurrentMode => _currentMode;

    public event EventHandler? ModeChanged;

    public double PanelWidth => _panelWidth;

    public event EventHandler? WidthChanged;

    public RightPanelService(IShellLayoutStore store)
    {
        _snapshotState = State.FromFeed(this, store.Snapshot);
        _snapshotState.ForEach(async (snapshot, ct) =>
        {
            if (snapshot is null) return;

            if (_currentMode != snapshot.RightPanelMode)
            {
                _currentMode = snapshot.RightPanelMode;
                ModeChanged?.Invoke(this, EventArgs.Empty);
            }

            if (!double.Equals(_panelWidth, snapshot.RightPanelWidth))
            {
                _panelWidth = snapshot.RightPanelWidth;
                WidthChanged?.Invoke(this, EventArgs.Empty);
            }
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
