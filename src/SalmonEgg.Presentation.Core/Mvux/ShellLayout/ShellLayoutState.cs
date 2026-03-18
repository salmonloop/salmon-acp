namespace SalmonEgg.Presentation.Core.Mvux.ShellLayout;

public sealed record ShellLayoutState(
    WindowMetrics WindowMetrics,
    LayoutPadding TitleBarPadding,
    double TitleBarInsetsHeight,
    RightPanelMode RightPanelMode,
    double RightPanelPreferredWidth,
    double NavOpenPaneLength,
    double NavCompactPaneLength,
    bool? UserNavOpenIntent)
{
    public static ShellLayoutState Default => new(
        new WindowMetrics(1280, 720, 1280, 720),
        new LayoutPadding(0, 0, 0, 0),
        48,
        RightPanelMode.None,
        320,
        300,
        72,
        null);
}
