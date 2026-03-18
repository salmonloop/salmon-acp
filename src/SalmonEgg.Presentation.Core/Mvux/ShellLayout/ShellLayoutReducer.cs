namespace SalmonEgg.Presentation.Core.Mvux.ShellLayout;

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
            LeftNavResizeRequested l => state with { NavOpenPaneLength = l.OpenPaneLength },
            _ => state
        };
        var snapshot = ShellLayoutPolicy.Compute(next);
        return new ShellLayoutReduced(next, snapshot);
    }
}
