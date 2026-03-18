namespace SalmonEgg.Presentation.Core.Mvux.ShellLayout;

public sealed record ShellLayoutSnapshot(
    NavigationPaneDisplayMode NavPaneDisplayMode,
    bool IsNavPaneOpen,
    double NavOpenPaneLength,
    double NavCompactPaneLength,
    bool SearchBoxVisible,
    double SearchBoxMinWidth,
    double SearchBoxMaxWidth,
    LayoutPadding TitleBarPadding,
    LayoutPadding NavViewPadding,
    double TitleBarHeight,
    bool RightPanelVisible,
    double RightPanelWidth,
    RightPanelMode RightPanelMode,
    bool IsNavResizerVisible,
    double LeftNavResizerLeft);
