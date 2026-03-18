using Xunit;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;
using System;

namespace SalmonEgg.Presentation.Core.Tests.ShellLayout;

public class ShellLayoutPolicyTests
{
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

    [Theory]
    [InlineData(800, NavigationPaneDisplayMode.Compact)]
    [InlineData(500, NavigationPaneDisplayMode.Minimal)]
    public void Policy_Allows_UserToOpenPane_InOverlayModes(double width, NavigationPaneDisplayMode expectedMode)
    {
        var state = ShellLayoutState.Default with
        {
            UserNavOpenIntent = true,
            WindowMetrics = new WindowMetrics(width, 700, width, 700)
        };

        var snapshot = ShellLayoutPolicy.Compute(state);

        Assert.Equal(expectedMode, snapshot.NavPaneDisplayMode);
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
}
