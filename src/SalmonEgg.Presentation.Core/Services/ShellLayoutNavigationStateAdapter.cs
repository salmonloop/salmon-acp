using System;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;
using Uno.Extensions.Reactive;

namespace SalmonEgg.Presentation.Services;

public sealed class ShellLayoutNavigationStateAdapter : INavigationPaneState, IDisposable
{
    private readonly IDisposable _subscription;
    private bool _isPaneOpen;
    public bool IsPaneOpen => _isPaneOpen;
    public event EventHandler? PaneStateChanged;

    public ShellLayoutNavigationStateAdapter(IShellLayoutStore store)
    {
        _subscription = store.Snapshot.ForEach((snapshot, ct) =>
        {
            if (snapshot is null) return ValueTask.CompletedTask;
            if (_isPaneOpen == snapshot.IsNavPaneOpen) return ValueTask.CompletedTask;
            _isPaneOpen = snapshot.IsNavPaneOpen;
            PaneStateChanged?.Invoke(this, EventArgs.Empty);
            return ValueTask.CompletedTask;
        });
    }

    public void Dispose() => _subscription?.Dispose();
}
