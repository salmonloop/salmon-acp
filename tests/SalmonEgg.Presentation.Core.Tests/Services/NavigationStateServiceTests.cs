using System;
using System.Threading;
using System.Threading.Tasks;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;
using SalmonEgg.Presentation.Services;
using Uno.Extensions.Reactive;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Services;

public class NavigationStateServiceTests
{
    [Fact]
    public async Task IsPaneOpen_ShouldChangeStateAndNotify()
    {
        using var store = new TestShellLayoutStore();
        using var service = new NavigationStateService(store);
        using var signal = new ManualResetEventSlim(false);
        var changedCount = 0;
        service.PaneStateChanged += (_, _) => changedCount++;
        service.PaneStateChanged += (_, _) => signal.Set();

        await store.Dispatch(new NavToggleRequested("test"));
        Assert.True(signal.Wait(TimeSpan.FromSeconds(1)));

        Assert.False(service.IsPaneOpen);
        Assert.Equal(1, changedCount);
    }

    [Fact]
    public async Task IsPaneOpen_ShouldNotNotifyIfValueIsSame()
    {
        using var store = new TestShellLayoutStore();
        using var service = new NavigationStateService(store);
        using var signal = new ManualResetEventSlim(false);
        service.PaneStateChanged += (_, _) => signal.Set();

        await store.Dispatch(new NavToggleRequested("test"));
        Assert.True(signal.Wait(TimeSpan.FromSeconds(1)));

        var changedCount = 0;
        service.PaneStateChanged += (_, _) => changedCount++;

        await store.Dispatch(new WindowMetricsChanged(1280, 720, 1280, 720));

        Assert.False(service.IsPaneOpen);
        Assert.Equal(0, changedCount);
    }

    private sealed class TestShellLayoutStore : IShellLayoutStore, IDisposable
    {
        private readonly IState<ShellLayoutState> _state;
        private readonly IState<ShellLayoutSnapshot> _snapshot;

        public TestShellLayoutStore()
        {
            _state = State.Value(new object(), () => ShellLayoutState.Default);
            _snapshot = State.Value(new object(), () => ShellLayoutPolicy.Compute(ShellLayoutState.Default));
        }

        public IFeed<ShellLayoutSnapshot> Snapshot => _snapshot;

        public ValueTask Dispatch(ShellLayoutAction action)
        {
            return _state.Update(s =>
            {
                var reduced = ShellLayoutReducer.Reduce(s!, action);
                _snapshot.Update(_ => reduced.Snapshot, default);
                return reduced.State;
            }, default);
        }

        public void Dispose()
        {
            _snapshot.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _state.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
