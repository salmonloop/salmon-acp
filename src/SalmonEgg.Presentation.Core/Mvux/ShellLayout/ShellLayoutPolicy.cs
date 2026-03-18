using System;

namespace SalmonEgg.Presentation.Core.Mvux.ShellLayout;

public static class ShellLayoutPolicy
{
    public static ShellLayoutSnapshot Compute(ShellLayoutState state)
    {
        var w = state.WindowMetrics.EffectiveWidth > 0 ? state.WindowMetrics.EffectiveWidth : state.WindowMetrics.Width;
        var mode = w >= 1000 ? NavigationPaneDisplayMode.Expanded : w >= 640 ? NavigationPaneDisplayMode.Compact : NavigationPaneDisplayMode.Minimal;
        var isOpen = mode switch
        {
            NavigationPaneDisplayMode.Expanded => state.UserNavOpenIntent != false,
            _ => state.UserNavOpenIntent == true
        };

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

        double navOpenLen = state.NavOpenPaneLength;
        double navCompactLen = state.NavCompactPaneLength;

        return new ShellLayoutSnapshot(
            mode,
            isOpen,
            navOpenLen,
            navCompactLen,
            searchVisible,
            minSearch,
            maxSearch,
            state.TitleBarPadding,
            new LayoutPadding(0, state.TitleBarInsetsHeight, 0, 0),
            state.TitleBarInsetsHeight,
            rightPanelVisible,
            rightPanelWidth,
            state.RightPanelMode,
            isOpen && mode == NavigationPaneDisplayMode.Expanded,
            isOpen ? navOpenLen - 6 : navCompactLen - 6);
    }
}
