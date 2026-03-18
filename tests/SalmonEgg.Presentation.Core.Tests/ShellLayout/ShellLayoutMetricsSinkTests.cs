using System.Threading.Tasks;
using Xunit;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;
using SalmonEgg.Presentation.Core.Services;
using Uno.Extensions.Reactive;

namespace SalmonEgg.Presentation.Core.Tests.ShellLayout;

public class ShellLayoutMetricsSinkTests
{
    [Fact]
    public async Task MetricsSink_Dispatches_WindowMetrics()
    {
        var store = new CapturingStore();
        var sink = new ShellLayoutMetricsSink(store);
        await sink.ReportWindowMetrics(100, 200, 80, 160);
        Assert.IsType<WindowMetricsChanged>(store.LastAction);
        var action = (WindowMetricsChanged)store.LastAction!;
        Assert.Equal(100, action.Width);
    }

    private sealed class CapturingStore : IShellLayoutStore
    {
        public IState<ShellLayoutState> State { get; } = Uno.Extensions.Reactive.State.Value(new object(), () => ShellLayoutState.Default);
        public IState<ShellLayoutSnapshot> Snapshot { get; } = Uno.Extensions.Reactive.State.Value(new object(), () => ShellLayoutPolicy.Compute(ShellLayoutState.Default));
        public ShellLayoutAction? LastAction { get; private set; }
        public ValueTask Dispatch(ShellLayoutAction action) { LastAction = action; return ValueTask.CompletedTask; }
    }
}
