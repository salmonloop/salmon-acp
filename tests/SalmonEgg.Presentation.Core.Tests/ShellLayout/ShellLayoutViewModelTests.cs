using System;
using System.Threading.Tasks;
using Xunit;
using Uno.Extensions.Reactive;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;
using SalmonEgg.Presentation.Core.ViewModels.ShellLayout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SalmonEgg.Presentation.Core.Tests.ShellLayout;

public class ShellLayoutViewModelTests
{
    [Fact]
    public async Task ViewModel_ProjectsSnapshot()
    {
        var snapshotState = State.Value(new object(), () => ShellLayoutPolicy.Compute(ShellLayoutState.Default));
        var store = new FakeShellLayoutStore(snapshotState);
        using var vm = new ShellLayoutViewModel(store);

        snapshotState.Update(_ => new ShellLayoutSnapshot(
            NavigationPaneDisplayMode.Compact, false, 300, 72,
            false, 0, 0, new LayoutPadding(4, 0, 4, 0), 60, false, 0), default);

        await Task.Delay(100);

        Assert.Equal(NavigationPaneDisplayMode.Compact, vm.NavPaneDisplayMode);
        Assert.False(vm.IsNavPaneOpen);
        Assert.Equal(60, vm.TitleBarHeight);
    }

    private sealed class FakeShellLayoutStore : IShellLayoutStore
    {
        public FakeShellLayoutStore(IState<ShellLayoutSnapshot> snapshot)
        {
            Snapshot = snapshot;
            State = Uno.Extensions.Reactive.State.Value(new object(), () => ShellLayoutState.Default);
        }
        public IState<ShellLayoutState> State { get; }
        public IState<ShellLayoutSnapshot> Snapshot { get; }
        public ValueTask Dispatch(ShellLayoutAction action) => ValueTask.CompletedTask;
    }
}
