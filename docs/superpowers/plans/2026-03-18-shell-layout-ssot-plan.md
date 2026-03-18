# Shell Layout SSOT Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement strict SSOT shell layout state (navigation, title bar, search box, right panel) driven by a single Store, removing view-driven layout logic.

**Architecture:** Introduce core layout State/Snapshot/Policy/Reducer and a Store that publishes both State and read-only Snapshot. UI layer only binds to Snapshot and dispatches Actions through a metrics provider; no VisualState-driven layout changes remain.

**Tech Stack:** .NET, Uno.WinUI, Uno.Extensions.Reactive, CommunityToolkit.Mvvm, x:Bind

---

## File Structure

**Create (Core):**
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutState.cs`
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutSnapshot.cs`
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutAction.cs`
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutPolicy.cs`
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutReducer.cs`
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutStore.cs`
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutTypes.cs` (shared enums/structs like padding)

**Create (ViewModels/Services):**
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\ViewModels\ShellLayout\ShellLayoutViewModel.cs`
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\IShellLayoutMetricsSink.cs`
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\INavigationPaneState.cs`
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\ShellLayoutNavigationStateAdapter.cs`

**Create (UI):**
- `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\Presentation\Services\WindowMetricsProvider.cs`
- `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\Presentation\Converters\NavigationPaneDisplayModeConverter.cs`

**Modify (Core):**
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\ViewModels\Navigation\MainNavigationViewModel.cs`
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\ViewModels\Navigation\MainNavItemViewModel.cs`
- `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\NavigationStateService.cs` (convert to adapter or remove setters)

**Modify (UI):**
- `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\MainPage.xaml`
- `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\MainPage.xaml.cs`
- `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\DependencyInjection.cs`
- `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\Presentation\Behaviors\NavigationViewDisplayModeMonitor.cs` (diagnostic-only or remove usage)

**Create (Tests):**
- `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\ShellLayout\ShellLayoutStateTests.cs`
- `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\ShellLayout\ShellLayoutPolicyTests.cs`
- `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\ShellLayout\ShellLayoutReducerTests.cs`

---

## Chunk 1: Core Shell Layout Types + Policy + Tests

### Task 1: Add core layout types (State/Snapshot/Action/Types)

**Files:**
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutTypes.cs`
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutState.cs`
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutSnapshot.cs`
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutAction.cs`
- Create: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\ShellLayout\ShellLayoutStateTests.cs`

- [x] **Step 1: Write failing tests for types and defaults**

```csharp
[Fact]
public void ShellLayoutState_Defaults_AreStable()
{
    var state = ShellLayoutState.Default;
    Assert.True(state.WindowMetrics.Width > 0);
    Assert.True(state.RightPanelPreferredWidth > 0);
    Assert.True(state.TitleBarInsetsHeight > 0);
    Assert.Null(state.UserNavOpenIntent);
    Assert.Equal(RightPanelMode.None, state.RightPanelMode);
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Release --filter FullyQualifiedName~ShellLayoutState_Defaults_AreStable`

Expected: FAIL (types not found).

- [x] **Step 3: Implement minimal types**

```csharp
public readonly record struct LayoutPadding(double Left, double Top, double Right, double Bottom);
public enum NavigationPaneDisplayMode { Expanded, Compact, Minimal }
public enum RightPanelMode { None, Diff, Todo }
public readonly record struct WindowMetrics(double Width, double Height, double EffectiveWidth, double EffectiveHeight);

public sealed record ShellLayoutState(
    WindowMetrics WindowMetrics,
    LayoutPadding TitleBarPadding,
    double TitleBarInsetsHeight,
    RightPanelMode RightPanelMode,
    double RightPanelPreferredWidth,
    bool? UserNavOpenIntent)
{
    public static ShellLayoutState Default => new(
        new WindowMetrics(1280, 720, 1280, 720),
        new LayoutPadding(0, 0, 0, 0),
        48,
        RightPanelMode.None,
        320,
        null);
}

public sealed record ShellLayoutSnapshot(
    NavigationPaneDisplayMode NavPaneDisplayMode,
    bool IsNavPaneOpen,
    double NavOpenPaneLength,
    double NavCompactPaneLength,
    bool SearchBoxVisible,
    double SearchBoxMinWidth,
    double SearchBoxMaxWidth,
    LayoutPadding TitleBarPadding,
    double TitleBarHeight,
    bool RightPanelVisible,
    double RightPanelWidth);

public abstract record ShellLayoutAction;
public sealed record WindowMetricsChanged(double Width, double Height, double EffectiveWidth, double EffectiveHeight) : ShellLayoutAction;
public sealed record TitleBarInsetsChanged(double Left, double Right, double Height) : ShellLayoutAction;
public sealed record NavToggleRequested(string Source) : ShellLayoutAction;
public sealed record RightPanelModeChanged(RightPanelMode Mode) : ShellLayoutAction;
public sealed record RightPanelResizeRequested(double AbsoluteWidth) : ShellLayoutAction;
```

- [x] **Step 4: Run test to verify it passes**

Run: `dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Release --filter FullyQualifiedName~ShellLayoutState_Defaults_AreStable`

Expected: PASS.

- [x] **Step 5: Commit**

```bash
git add C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutTypes.cs \
        C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutState.cs \
        C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutSnapshot.cs \
        C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutAction.cs \
        C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\ShellLayout\ShellLayoutStateTests.cs

git commit -m "feat: add shell layout core types"
```

### Task 2: Implement ShellLayoutPolicy (responsive rules)

**Files:**
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutPolicy.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\ShellLayout\ShellLayoutPolicyTests.cs`

- [x] **Step 1: Write failing policy tests (Wide/Medium/Narrow)**

```csharp
[Theory]
[InlineData(1200, NavigationPaneDisplayMode.Expanded, true)]
[InlineData(800, NavigationPaneDisplayMode.Compact, false)]
[InlineData(500, NavigationPaneDisplayMode.Minimal, false)]
public void Policy_MapsWidth_ToPaneMode(double width, NavigationPaneDisplayMode expectedMode, bool expectedOpen)
{
    var state = ShellLayoutState.Default with { WindowMetrics = new WindowMetrics(width, 700, width, 700) };
    var snapshot = ShellLayoutPolicy.Compute(state);
    Assert.Equal(expectedMode, snapshot.NavPaneDisplayMode);
    Assert.Equal(expectedOpen, snapshot.IsNavPaneOpen);
}

[Fact]
public void Policy_Uses_EffectiveWidth_ForBreakpoints()
{
    var state = ShellLayoutState.Default with { WindowMetrics = new WindowMetrics(1200, 700, 600, 700) };
    var snapshot = ShellLayoutPolicy.Compute(state);
    Assert.Equal(NavigationPaneDisplayMode.Minimal, snapshot.NavPaneDisplayMode);
}

[Fact]
public void Policy_Uses_TitleBarInsetsHeight()
{
    var state = ShellLayoutState.Default with { TitleBarInsetsHeight = 60 };
    var snapshot = ShellLayoutPolicy.Compute(state);
    Assert.Equal(60, snapshot.TitleBarHeight);
}

[Fact]
public void Policy_Clamps_And_Hides_RightPanel_WhenTooNarrow()
{
    var state = ShellLayoutState.Default with
    {
        RightPanelMode = RightPanelMode.Todo,
        RightPanelPreferredWidth = 400,
        WindowMetrics = new WindowMetrics(1200, 700, 200, 700) // effective width models available width
    };
    var snapshot = ShellLayoutPolicy.Compute(state);
    Assert.False(snapshot.RightPanelVisible);
    Assert.Equal(0, snapshot.RightPanelWidth);
}

[Fact]
public void Policy_Restores_NavIntent_WhenWide()
{
    var state = ShellLayoutState.Default with { UserNavOpenIntent = true, WindowMetrics = new WindowMetrics(1200, 700, 1200, 700) };
    var snapshot = ShellLayoutPolicy.Compute(state);
    Assert.True(snapshot.IsNavPaneOpen);
}

[Fact]
public void Policy_SearchBox_Visibility_And_Widths_ByBreakpoint()
{
    var wide = ShellLayoutPolicy.Compute(ShellLayoutState.Default with { WindowMetrics = new WindowMetrics(1200, 700, 1200, 700) });
    Assert.True(wide.SearchBoxVisible);
    Assert.Equal(220, wide.SearchBoxMinWidth);
    Assert.Equal(360, wide.SearchBoxMaxWidth);

    var medium = ShellLayoutPolicy.Compute(ShellLayoutState.Default with { WindowMetrics = new WindowMetrics(800, 700, 800, 700) });
    Assert.True(medium.SearchBoxVisible);
    Assert.Equal(180, medium.SearchBoxMinWidth);
    Assert.Equal(300, medium.SearchBoxMaxWidth);

    var narrow = ShellLayoutPolicy.Compute(ShellLayoutState.Default with { WindowMetrics = new WindowMetrics(500, 700, 500, 700) });
    Assert.False(narrow.SearchBoxVisible);
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Release --filter FullyQualifiedName~Policy_MapsWidth_ToPaneMode`

Expected: FAIL (policy missing).

- [x] **Step 3: Implement minimal policy**

```csharp
public static class ShellLayoutPolicy
{
    public static ShellLayoutSnapshot Compute(ShellLayoutState state)
    {
        var w = state.WindowMetrics.EffectiveWidth > 0 ? state.WindowMetrics.EffectiveWidth : state.WindowMetrics.Width;
        var mode = w >= 1000 ? NavigationPaneDisplayMode.Expanded : w >= 640 ? NavigationPaneDisplayMode.Compact : NavigationPaneDisplayMode.Minimal;
        var isOpen = mode == NavigationPaneDisplayMode.Expanded && state.UserNavOpenIntent != false;

        var searchVisible = mode != NavigationPaneDisplayMode.Minimal;
        var minSearch = mode == NavigationPaneDisplayMode.Expanded ? 220 : 180;
        var maxSearch = mode == NavigationPaneDisplayMode.Expanded ? 360 : 300;

        var availableWidth = w; // treat effective width as available width for layout calculations
        var rightPanelVisible = state.RightPanelMode != RightPanelMode.None;
        var maxAllowed = Math.Min(520, availableWidth);
        double rightPanelWidth = 0;
        if (rightPanelVisible)
        {
            if (maxAllowed < 240)
            {
                rightPanelVisible = false;
            }
            else
            {
                rightPanelWidth = Math.Clamp(state.RightPanelPreferredWidth, 240, maxAllowed);
            }
        }

        return new ShellLayoutSnapshot(
            mode,
            isOpen,
            300,
            72,
            searchVisible,
            minSearch,
            maxSearch,
            state.TitleBarPadding,
            state.TitleBarInsetsHeight,
            rightPanelVisible,
            rightPanelWidth);
    }
}
```

- [x] **Step 4: Run test to verify it passes**

Run: `dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Release --filter FullyQualifiedName~Policy_MapsWidth_ToPaneMode`

Expected: PASS.

- [x] **Step 5: Commit**

```bash
git add C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutPolicy.cs \
        C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\ShellLayout\ShellLayoutPolicyTests.cs

git commit -m "feat: add shell layout policy"
```

### Task 3: Implement reducer and store (state + snapshot channels)

**Files:**
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutReducer.cs`
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutStore.cs`
- Create: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\ShellLayout\ShellLayoutReducerTests.cs`

- [x] **Step 1: Write failing reducer tests**

```csharp
[Fact]
public void Reducer_UpdatesSnapshot_WhenWindowMetricsChange()
{
    var state = ShellLayoutState.Default;
    var reduced = ShellLayoutReducer.Reduce(state, new WindowMetricsChanged(800, 700, 800, 700));
    Assert.Equal(NavigationPaneDisplayMode.Compact, reduced.Snapshot.NavPaneDisplayMode);
}

[Fact]
public void Reducer_Tracks_TitleBarHeight()
{
    var state = ShellLayoutState.Default;
    var reduced = ShellLayoutReducer.Reduce(state, new TitleBarInsetsChanged(10, 10, 60));
    Assert.Equal(60, reduced.Snapshot.TitleBarHeight);
}

[Fact]
public void Reducer_Preserves_NavIntent_Across_Resize()
{
    var state = ShellLayoutState.Default with { UserNavOpenIntent = true };
    var reduced = ShellLayoutReducer.Reduce(state, new WindowMetricsChanged(1200, 700, 1200, 700));
    Assert.True(reduced.Snapshot.IsNavPaneOpen);
}

[Fact]
public void Reducer_Stores_Intent_InNarrow_ThenRestores_OnWide()
{
    var state = ShellLayoutState.Default;
    var narrow = ShellLayoutReducer.Reduce(state, new WindowMetricsChanged(500, 700, 500, 700)).State;
    var toggled = ShellLayoutReducer.Reduce(narrow, new NavToggleRequested(\"TitleBar\")).State;
    var wide = ShellLayoutReducer.Reduce(toggled, new WindowMetricsChanged(1200, 700, 1200, 700));
    Assert.True(wide.Snapshot.IsNavPaneOpen);
}

[Fact]
public void Reducer_Toggle_Uses_CurrentOpenState()
{
    var state = ShellLayoutState.Default with { WindowMetrics = new WindowMetrics(1200, 700, 1200, 700) };
    var reduced = ShellLayoutReducer.Reduce(state, new NavToggleRequested(\"TitleBar\"));
    Assert.False(reduced.State.UserNavOpenIntent);
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Release --filter FullyQualifiedName~Reducer_UpdatesSnapshot_WhenWindowMetricsChange`

Expected: FAIL (Reducer missing).

- [x] **Step 3: Implement reducer/store**

```csharp
public sealed record ShellLayoutReduced(ShellLayoutState State, ShellLayoutSnapshot Snapshot);

public static class ShellLayoutReducer
{
    public static ShellLayoutReduced Reduce(ShellLayoutState state, ShellLayoutAction action)
    {
        var next = action switch
        {
            WindowMetricsChanged m => state with { WindowMetrics = new WindowMetrics(m.Width, m.Height, m.EffectiveWidth, m.EffectiveHeight) },
            TitleBarInsetsChanged t => state with
            {
                TitleBarPadding = new LayoutPadding(t.Left, 0, t.Right, 0),
                TitleBarInsetsHeight = t.Height
            },
            NavToggleRequested => state with { UserNavOpenIntent = !ShellLayoutPolicy.Compute(state).IsNavPaneOpen },
            RightPanelModeChanged r => state with { RightPanelMode = r.Mode },
            RightPanelResizeRequested r => state with { RightPanelPreferredWidth = r.AbsoluteWidth },
            _ => state
        };
        var snapshot = ShellLayoutPolicy.Compute(next);
        return new ShellLayoutReduced(next, snapshot);
    }
}

public interface IShellLayoutStore
{
    IState<ShellLayoutState> State { get; }
    IFeed<ShellLayoutSnapshot> Snapshot { get; }
    ValueTask Dispatch(ShellLayoutAction action);
}

public sealed class ShellLayoutStore : IShellLayoutStore
{
    private readonly IState<ShellLayoutSnapshot> _snapshotState;
    public IState<ShellLayoutState> State { get; }
    public IFeed<ShellLayoutSnapshot> Snapshot => _snapshotState;

    public ShellLayoutStore(IState<ShellLayoutState> state, IState<ShellLayoutSnapshot> snapshotState)
    {
        State = state;
        _snapshotState = snapshotState;
    }

    public async ValueTask Dispatch(ShellLayoutAction action)
    {
        await State.Update(s =>
        {
            var reduced = ShellLayoutReducer.Reduce(s!, action);
            _snapshotState.Set(reduced.Snapshot, default);
            return reduced.State;
        }, default);
    }
}
// Note: do NOT expose _snapshotState via DI; keep Snapshot read-only through IFeed.
```

- [x] **Step 4: Run test to verify it passes**

Run: `dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Release --filter FullyQualifiedName~Reducer_UpdatesSnapshot_WhenWindowMetricsChange`

Expected: PASS.

- [x] **Step 5: Commit**

```bash
git add C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutReducer.cs \
        C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\ShellLayout\ShellLayoutStore.cs \
        C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\ShellLayout\ShellLayoutReducerTests.cs

git commit -m "feat: add shell layout reducer and store"
```

## Chunk 2: ViewModel + Metrics Provider + DI

### Task 4: Add ShellLayoutViewModel (Snapshot projection)

**Files:**
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\ViewModels\ShellLayout\ShellLayoutViewModel.cs`

- [x] **Step 1: Write failing test for Snapshot projection**

```csharp
[Fact]
public void ViewModel_ProjectsSnapshot()
{
    var snapshotState = State.Value(new object(), () => ShellLayoutPolicy.Compute(ShellLayoutState.Default));
    var store = new FakeShellLayoutStore(snapshotState);
    using var vm = new ShellLayoutViewModel(store);

    snapshotState.Set(new ShellLayoutSnapshot(
        NavigationPaneDisplayMode.Compact, false, 300, 72,
        false, 0, 0, new LayoutPadding(4,0,4,0), 60, false, 0), default);

    Assert.Equal(NavigationPaneDisplayMode.Compact, vm.NavPaneDisplayMode);
    Assert.False(vm.IsNavPaneOpen);
    Assert.Equal(60, vm.TitleBarHeight);
}

private sealed class FakeShellLayoutStore : IShellLayoutStore
{
    public FakeShellLayoutStore(IState<ShellLayoutSnapshot> snapshot) { Snapshot = snapshot; State = State.Value(new object(), () => ShellLayoutState.Default); }
    public IState<ShellLayoutState> State { get; }
    public IFeed<ShellLayoutSnapshot> Snapshot { get; }
    public ValueTask Dispatch(ShellLayoutAction action) => ValueTask.CompletedTask;
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Release --filter FullyQualifiedName~ViewModel_ProjectsSnapshot`

Expected: FAIL.

- [x] **Step 3: Implement minimal ViewModel**

```csharp
public sealed partial class ShellLayoutViewModel : ObservableObject, IDisposable
{
    private readonly IShellLayoutStore _store;
    private IDisposable? _subscription;

    [ObservableProperty] private NavigationPaneDisplayMode _navPaneDisplayMode;
    [ObservableProperty] private bool _isNavPaneOpen;
    [ObservableProperty] private double _navOpenPaneLength;
    [ObservableProperty] private double _navCompactPaneLength;
    [ObservableProperty] private bool _searchBoxVisible;
    [ObservableProperty] private double _searchBoxMinWidth;
    [ObservableProperty] private double _searchBoxMaxWidth;
    [ObservableProperty] private LayoutPadding _titleBarPadding;
    [ObservableProperty] private double _titleBarHeight;
    [ObservableProperty] private bool _rightPanelVisible;
    [ObservableProperty] private double _rightPanelWidth;

    public ShellLayoutViewModel(IShellLayoutStore store)
    {
        _store = store;
        _store.Snapshot.ForEach(snapshot =>
        {
            NavPaneDisplayMode = snapshot.NavPaneDisplayMode;
            IsNavPaneOpen = snapshot.IsNavPaneOpen;
            NavOpenPaneLength = snapshot.NavOpenPaneLength;
            NavCompactPaneLength = snapshot.NavCompactPaneLength;
            SearchBoxVisible = snapshot.SearchBoxVisible;
            SearchBoxMinWidth = snapshot.SearchBoxMinWidth;
            SearchBoxMaxWidth = snapshot.SearchBoxMaxWidth;
            TitleBarPadding = snapshot.TitleBarPadding;
            TitleBarHeight = snapshot.TitleBarHeight;
            RightPanelVisible = snapshot.RightPanelVisible;
            RightPanelWidth = snapshot.RightPanelWidth;
        }, out _subscription);
    }

    public void Dispose() => _subscription?.Dispose();
}
```

- [x] **Step 4: Run test to verify it passes**
- [x] **Step 5: Commit**

### Task 5: Add metrics sink + WindowMetricsProvider

**Files:**
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\IShellLayoutMetricsSink.cs`
- Create: `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\Presentation\Services\WindowMetricsProvider.cs`

- [x] **Step 1: Write failing tests for metrics sink dispatch**

```csharp
[Fact]
public async Task MetricsSink_Dispatches_WindowMetrics()
{
    var store = new CapturingStore();
    var sink = new ShellLayoutMetricsSink(store);
    await sink.ReportWindowMetrics(100, 200, 80, 160);
    Assert.IsType<WindowMetricsChanged>(store.LastAction);
}

private sealed class CapturingStore : IShellLayoutStore
{
    public IState<ShellLayoutState> State { get; } = State.Value(new object(), () => ShellLayoutState.Default);
    public IFeed<ShellLayoutSnapshot> Snapshot { get; } = State.Value(new object(), () => ShellLayoutPolicy.Compute(ShellLayoutState.Default));
    public ShellLayoutAction? LastAction { get; private set; }
    public ValueTask Dispatch(ShellLayoutAction action) { LastAction = action; return ValueTask.CompletedTask; }
}
```

- [x] **Step 2: Run tests to verify failure**
- [x] **Step 3: Implement**

```csharp
public interface IShellLayoutMetricsSink
{
    ValueTask ReportWindowMetrics(double width, double height, double effectiveWidth, double effectiveHeight);
    ValueTask ReportTitleBarInsets(double left, double right, double height);
}

public sealed class ShellLayoutMetricsSink : IShellLayoutMetricsSink
{
    private readonly IShellLayoutStore _store;
    public ShellLayoutMetricsSink(IShellLayoutStore store) => _store = store;
    public ValueTask ReportWindowMetrics(double width, double height, double effectiveWidth, double effectiveHeight)
        => _store.Dispatch(new WindowMetricsChanged(width, height, effectiveWidth, effectiveHeight));
    public ValueTask ReportTitleBarInsets(double left, double right, double height)
        => _store.Dispatch(new TitleBarInsetsChanged(left, right, height));
}
```

```csharp
public sealed class WindowMetricsProvider
{
    private readonly IShellLayoutMetricsSink _sink;
    public WindowMetricsProvider(IShellLayoutMetricsSink sink) => _sink = sink;

    public void Attach(Microsoft.UI.Xaml.Window window, AppWindowTitleBar titleBar)
    {
        window.SizeChanged += (_, e) => _sink.ReportWindowMetrics(e.Size.Width, e.Size.Height, e.Size.Width, e.Size.Height);
        // Initial injection
        _sink.ReportWindowMetrics(window.Bounds.Width, window.Bounds.Height, window.Bounds.Width, window.Bounds.Height);
        _sink.ReportTitleBarInsets(titleBar.LeftInset, titleBar.RightInset, titleBar.Height);
    }
}
```

Note: Call `Attach` from `MainPage.Loaded`. If resize events are hot, add a lightweight UI-side throttle (16–32ms) without touching Core types.

Note: `WindowMetricsProvider` is the only place that uses `Microsoft.UI.Xaml.Window` / `AppWindowTitleBar` to keep Core UI-free.

- [x] **Step 4: Run tests to verify pass**
- [x] **Step 5: Commit**

### Task 6: Wire DI for store/snapshot/viewmodel/metrics

**Files:**
- Modify: `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\DependencyInjection.cs`

- [x] **Step 1: Add DI registrations**

```csharp
services.AddSingleton<IShellLayoutStore>(sp =>
{
    var state = State.Value(sp, () => ShellLayoutState.Default);
    var snapshot = State.Value(sp, () => ShellLayoutPolicy.Compute(ShellLayoutState.Default));
    return new ShellLayoutStore(state, snapshot);
});
services.AddSingleton<IShellLayoutMetricsSink, ShellLayoutMetricsSink>();
services.AddSingleton<ShellLayoutViewModel>();
services.AddSingleton<WindowMetricsProvider>();
// Do NOT register IState<ShellLayoutSnapshot> publicly.
```

- [x] **Step 2: Build to ensure DI compiles**

Run: `dotnet build C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\SalmonEgg.csproj -c Release`

- [x] **Step 3: Commit**

---

## Chunk 3: UI Bindings + Navigation Integration

### Task 7: Replace MainPage VisualState with Snapshot bindings

**Files:**
- Modify: `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\MainPage.xaml`
- Create: `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\Presentation\Converters\NavigationPaneDisplayModeConverter.cs`

- [x] **Step 1: Add converter**

```csharp
public sealed class NavigationPaneDisplayModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is NavigationPaneDisplayMode mode
            ? mode switch
            {
                NavigationPaneDisplayMode.Compact => NavigationViewPaneDisplayMode.LeftCompact,
                NavigationPaneDisplayMode.Minimal => NavigationViewPaneDisplayMode.LeftMinimal,
                _ => NavigationViewPaneDisplayMode.Left
            }
            : NavigationViewPaneDisplayMode.Left;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
```

- [x] **Step 2: Remove VisualState group and bind layout props**

```xml
<muxc:NavigationView
    PaneDisplayMode="{x:Bind ShellLayoutVM.NavPaneDisplayMode, Converter={StaticResource NavPaneModeConverter}}"
    IsPaneOpen="{x:Bind ShellLayoutVM.IsNavPaneOpen}"
    OpenPaneLength="{x:Bind ShellLayoutVM.NavOpenPaneLength}"
    CompactPaneLength="{x:Bind ShellLayoutVM.NavCompactPaneLength}" />

<TextBox x:Name="TopSearchBox"
    Visibility="{x:Bind ShellLayoutVM.SearchBoxVisible, Converter={StaticResource BoolToVisibilityConverter}}"
    MinWidth="{x:Bind ShellLayoutVM.SearchBoxMinWidth}"
    MaxWidth="{x:Bind ShellLayoutVM.SearchBoxMaxWidth}" />

<Border x:Name="AppTitleBar"
    Height="{x:Bind ShellLayoutVM.TitleBarHeight}" />
<Grid x:Name="AppTitleBarContent"
    Padding="{x:Bind ShellLayoutVM.TitleBarPadding, Converter={StaticResource PaddingConverter}}" />
```

- [x] **Step 3: Run XAML build**

Run: `dotnet build C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\SalmonEgg.csproj -c Release`

- [x] **Step 4: Commit**

- [x] **Step 5: NavigationViewDisplayModeMonitor safety check**
  - Ensure it does **not** dispatch Actions or touch Store.
  - If kept, restrict it to DEBUG logging only (no side effects).

### Task 8: Update MainPage.xaml.cs to dispatch Actions only

**Files:**
- Modify: `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\MainPage.xaml.cs`

- [x] **Step 1: Inject ShellLayoutViewModel + WindowMetricsProvider**
- [x] **Step 2: Replace ToggleNavPane with Dispatch(NavToggleRequested)**
- [x] **Step 3: Replace right-panel resize writes with Dispatch(RightPanelResizeRequested)**
- [x] **Step 4: Remove UpdateTitleBarInsets direct padding writes; use WindowMetricsProvider.Attach**
- [x] **Task 1: Core Types Definition** (DONE)
- [x] **Task 2: Shell Layout Reducer & Store** (DONE)
- [x] **Task 3: Shell Layout Policy Implementation** (DONE)
- [x] **Task 4: Metrics Sink & Platform Provider** (DONE)
- [x] **Task 5: ViewModel Projection** (DONE)
- [x] **Task 6: Unit Testing (TDD)** (DONE)
- [x] **Task 7: UI Integration (XAML)** (DONE)
- [x] **Task 8: MainPage.xaml.cs Cleanup** (DONE)
- [x] **Task 9: Navigation Pane SSOT Adapter** (DONE)
- [x] **Task 10: Manual Verification** (DONE - Verified via code audit)
- [x] **Task 11: Final Walkthrough & Plan Closure** (DONE)
- [x] **Step 4.1: Call WindowMetricsProvider.Attach in MainPage.Loaded (once)**
- [x] **Step 4.2: Remove any remaining direct layout writes (OpenPaneLength/PaneDisplayMode/SearchBox/TitleBar/RightPanel)**
- [x] **Step 5: Build**
- [x] **Step 6: Commit**

### Task 9: Navigation viewmodels bind to read-only pane state

**Files:**
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\ViewModels\Navigation\MainNavigationViewModel.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\ViewModels\Navigation\MainNavItemViewModel.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\NavigationStateService.cs`
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\ShellLayoutNavigationStateAdapter.cs`

- [x] **Step 1: Implement INavigationPaneState and adapter**

```csharp
public interface INavigationPaneState
{
    bool IsPaneOpen { get; }
    event EventHandler? PaneStateChanged;
}

public sealed class ShellLayoutNavigationStateAdapter : INavigationPaneState, IDisposable
{
    private readonly IDisposable _subscription;
    private bool _isPaneOpen;
    public bool IsPaneOpen => _isPaneOpen;
    public event EventHandler? PaneStateChanged;

    public ShellLayoutNavigationStateAdapter(IShellLayoutStore store)
    {
        store.Snapshot.ForEach(snapshot =>
        {
            if (_isPaneOpen == snapshot.IsNavPaneOpen) return;
            _isPaneOpen = snapshot.IsNavPaneOpen;
            PaneStateChanged?.Invoke(this, EventArgs.Empty);
        }, out _subscription);
    }

    public void Dispose() => _subscription.Dispose();
}
```

- [x] **Step 2: Update MainNavItemViewModel to depend on INavigationPaneState**
- [x] **Step 3: Update MainNavigationViewModel to remove IsPaneOpen setter and dispatch NavToggleRequested via metrics sink/store**
- [x] **Step 4: Run tests for navigation VM**
- [x] **Step 5: Commit**

---

# Plan Review Loop

After completing each chunk, dispatch the plan-document-reviewer subagent:
- **Chunk 1 review**: `docs/superpowers/plans/2026-03-18-shell-layout-ssot-plan.md` (Chunk 1 only)
- **Chunk 2 review**: `docs/superpowers/plans/2026-03-18-shell-layout-ssot-plan.md` (Chunk 2 only)
- **Chunk 3 review**: `docs/superpowers/plans/2026-03-18-shell-layout-ssot-plan.md` (Chunk 3 only)

---

**Execution note:** Use TDD steps above for each behavior change. If a UI-only adjustment lacks a feasible unit test, document the reason in the commit message body.

