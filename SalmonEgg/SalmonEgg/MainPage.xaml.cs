using System.Collections.Generic;
using System.ComponentModel;
#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
#endif
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
#if __SKIA__
using Microsoft.UI.Input;
using Windows.Graphics;
#endif
using SalmonEgg.Presentation.ViewModels.Navigation;
using SalmonEgg.Presentation.ViewModels.Settings;
using SalmonEgg.Presentation.Views;
using SalmonEgg.Presentation.Views.Chat;

namespace SalmonEgg;

public sealed partial class MainPage : Page
{
    private const double SubMenuMinWidth = 200;
    private const double SubMenuMaxWidth = 420;
    private const double RightPanelMinWidth = 240;
    private const double RightPanelMaxWidth = 520;
    private bool _isResizingSubMenu;
    private double _subMenuResizeStartX;
    private double _subMenuResizeStartWidth;
    private bool _isResizingRightPanel;
    private double _rightPanelResizeStartX;
    private double _rightPanelResizeStartWidth;
    private string? _activeRightPanel;
    private bool _isLeftNavCollapsed;
    private bool _hasNonClientDragRegions;
#if WINDOWS
    private AppWindowTitleBar? _appWindowTitleBar;
#endif

    public AppPreferencesViewModel Preferences { get; }
    public SidebarViewModel SidebarVM { get; }



    // 公开暴露导航列表，以便子页面可以触发全局导航切换
    public ListView MainRailNavList => MainRailNav;
    public ListView BottomRailNavList => BottomRailNav;
    public ListView SettingsSubMenuListView => SettingsSubMenuList;

    public MainPage()
    {
        App.BootLog("MainPage: ctor start");
        // 1. 在初始化组件前获取 ViewModel，确保 x:Bind 绑定正常
        Preferences = App.ServiceProvider.GetRequiredService<AppPreferencesViewModel>();
        SidebarVM = App.ServiceProvider.GetRequiredService<SidebarViewModel>();

        this.InitializeComponent();
        App.BootLog("MainPage: InitializeComponent done");

        Loaded += OnMainPageLoaded;
        ContentFrame.Navigated += OnContentFrameNavigated;
        if (AppTitleBar != null)
        {
            AppTitleBar.Loaded += OnTitleBarLoaded;
            AppTitleBar.SizeChanged += OnTitleBarSizeChanged;
        }

        if (TitleBarDragLeft != null)
        {
            TitleBarDragLeft.SizeChanged += OnTitleBarSizeChanged;
        }

        if (TitleBarDragRight != null)
        {
            TitleBarDragRight.SizeChanged += OnTitleBarSizeChanged;
        }

#if !WINDOWS
        // Cross-platform fallback "Mica-like" backdrop.
        if (Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("AppBackdropBrush", out var brush) && brush is Brush b)
        {
            Background = b;
        }
#endif

        // 2. 监听全局设置变化（如动画开关、主题、背景材质）
        Preferences.PropertyChanged += OnPreferencesPropertyChanged;

        // 3. 初始化主题与动画状态
        ApplyTheme();
        ApplyBackdrop();
        UpdateNavigationTransitions();
        App.BootLog("MainPage: transitions updated");

        // 4. 初始化导航默认选中项（避免 XAML 初始化期间 SelectionChanged 触发导致 NRE）
        MainRailNav.SelectionChanged -= OnMainRailNavSelectionChanged;
        BottomRailNav.SelectionChanged -= OnBottomRailNavSelectionChanged;
        SettingsSubMenuList.SelectionChanged -= OnSubMenuSelectionChanged;
        try
        {
            MainRailNav.SelectedItem = ChatNavItem;
            BottomRailNav.SelectedIndex = -1;
            SubMenuColumn.Visibility = Visibility.Visible;

            ChatSubNavPanel.Visibility = Visibility.Visible;
            SettingsSubNavPanel.Visibility = Visibility.Collapsed;

            SettingsSubMenuList.SelectedIndex = -1;
        }
        finally
        {
            MainRailNav.SelectionChanged += OnMainRailNavSelectionChanged;
            BottomRailNav.SelectionChanged += OnBottomRailNavSelectionChanged;
            SettingsSubMenuList.SelectionChanged += OnSubMenuSelectionChanged;
        }

        // 5. 启动后默认进入对话界面
        NavigateTo(typeof(ChatView));
        UpdateRightPanelAvailability(true);
        App.BootLog("MainPage: navigated to ChatView");
    }

    public void NavigateToSettingsSubPage(string key)
    {
        // Ensure Settings panel is visible.
        SubMenuColumn.Visibility = Visibility.Visible;
        ChatSubNavPanel.Visibility = Visibility.Collapsed;
        SettingsSubNavPanel.Visibility = Visibility.Visible;
        UpdateRightPanelAvailability(false);

        SettingsSubMenuList.SelectionChanged -= OnSubMenuSelectionChanged;
        try
        {
            var index = key switch
            {
                "General" => 0,
                "Appearance" => 1,
                "AgentAcp" => 2,
                "DataStorage" => 3,
                "Shortcuts" => 4,
                "Diagnostics" => 5,
                "About" => 6,
                _ => 0
            };
            SettingsSubMenuList.SelectedIndex = index;
        }
        finally
        {
            SettingsSubMenuList.SelectionChanged += OnSubMenuSelectionChanged;
        }

        var pageType = key switch
        {
            "General" => typeof(SalmonEgg.Presentation.Views.GeneralSettingsPage),
            "Appearance" => typeof(SalmonEgg.Presentation.Views.Settings.AppearanceSettingsPage),
            "AgentAcp" => typeof(SalmonEgg.Presentation.Views.Settings.AcpConnectionSettingsPage),
            "DataStorage" => typeof(SalmonEgg.Presentation.Views.Settings.DataStorageSettingsPage),
            "Shortcuts" => typeof(SalmonEgg.Presentation.Views.Settings.ShortcutsSettingsPage),
            "Diagnostics" => typeof(SalmonEgg.Presentation.Views.Settings.DiagnosticsSettingsPage),
            "About" => typeof(SalmonEgg.Presentation.Views.Settings.AboutPage),
            _ => typeof(SalmonEgg.Presentation.Views.GeneralSettingsPage)
        };

        NavigateTo(pageType);
        ApplyLeftNavVisibility();
    }

    private void OnPreferencesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Preferences.IsAnimationEnabled))
        {
            UpdateNavigationTransitions();
        }

        if (e.PropertyName == nameof(Preferences.Theme))
        {
            ApplyTheme();
        }

        if (e.PropertyName == nameof(Preferences.Backdrop))
        {
            ApplyBackdrop();
        }
    }

    private void ApplyTheme()
    {
        var theme = Preferences.Theme?.Trim();
        var requested = theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (App.MainWindowInstance?.Content is FrameworkElement root && root.RequestedTheme != requested)
        {
            root.RequestedTheme = requested;
        }
    }

    private void ApplyBackdrop()
    {
#if WINDOWS
        try
        {
            var window = App.MainWindowInstance;
            if (window == null)
            {
                return;
            }

            var pref = (Preferences.Backdrop ?? "System").Trim();
            Microsoft.UI.Xaml.Media.SystemBackdrop? backdrop = pref switch
            {
                "Mica" => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)
                    ? new Microsoft.UI.Xaml.Media.MicaBackdrop()
                    : OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)
                        ? new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop()
                        : null,
                "Acrylic" => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)
                    ? new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop()
                    : null,
                "Solid" => null,
                _ => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)
                    ? new Microsoft.UI.Xaml.Media.MicaBackdrop()
                    : OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)
                        ? new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop()
                        : null
            };

            window.SystemBackdrop = backdrop;
        }
        catch
        {
        }
#endif
    }

    private void UpdateNavigationTransitions()
    {
        // 根据全局设置动态开启或关闭 Frame 的过渡动画
        if (Preferences.IsAnimationEnabled)
        {
            ContentFrame.ContentTransitions = new TransitionCollection
            {
#if WINDOWS
                new NavigationThemeTransition { DefaultNavigationTransitionInfo = new EntranceNavigationTransitionInfo() }
#else
                new EntranceThemeTransition()
#endif
            };
        }
        else
        {
            ContentFrame.ContentTransitions = null;
        }
    }

    private void NavigateTo(Type pageType)
    {
#if WINDOWS
        var transition = Preferences.IsAnimationEnabled
            ? (NavigationTransitionInfo)new EntranceNavigationTransitionInfo()
            : new SuppressNavigationTransitionInfo();
        ContentFrame.Navigate(pageType, null, transition);
#else
        ContentFrame.Navigate(pageType);
#endif
    }

    private void OnMainRailNavSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BottomRailNav is null || SubMenuColumn is null || ContentFrame is null)
        {
            return;
        }

        if (MainRailNav.SelectedItem is ListViewItem item && item == ChatNavItem)
        {
            // 互斥逻辑：选中上方导航时，清除下方导航的选中状态
            BottomRailNav.SelectionChanged -= OnBottomRailNavSelectionChanged;
            BottomRailNav.SelectedIndex = -1;
            BottomRailNav.SelectionChanged += OnBottomRailNavSelectionChanged;

            // 聊天界面显示项目/会话子导航
            SubMenuColumn.Visibility = Visibility.Visible;
            ChatSubNavPanel.Visibility = Visibility.Visible;
            SettingsSubNavPanel.Visibility = Visibility.Collapsed;

            SettingsSubMenuList.SelectionChanged -= OnSubMenuSelectionChanged;
            SettingsSubMenuList.SelectedIndex = -1;
            SettingsSubMenuList.SelectionChanged += OnSubMenuSelectionChanged;
            NavigateTo(typeof(ChatView));
            UpdateRightPanelAvailability(true);
            ApplyLeftNavVisibility();
        }
    }

    private void OnBottomRailNavSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BottomRailNav.SelectedItem is ListViewItem item && item == SettingsNavItem)
        {
            // 互斥逻辑：选中下方导航时，清除上方导航的选中状态
            MainRailNav.SelectionChanged -= OnMainRailNavSelectionChanged;
            MainRailNav.SelectedIndex = -1;
            MainRailNav.SelectionChanged += OnMainRailNavSelectionChanged;

            // 展开二级导航栏（设置），并默认加载外观设置
            SubMenuColumn.Visibility = Visibility.Visible;
            ChatSubNavPanel.Visibility = Visibility.Collapsed;
            SettingsSubNavPanel.Visibility = Visibility.Visible;

            SettingsSubMenuList.SelectionChanged -= OnSubMenuSelectionChanged;
            SettingsSubMenuList.SelectedIndex = 0; // "常规"
            SettingsSubMenuList.SelectionChanged += OnSubMenuSelectionChanged;
            NavigateTo(typeof(SalmonEgg.Presentation.Views.GeneralSettingsPage));
            UpdateRightPanelAvailability(false);
            ApplyLeftNavVisibility();
        }
    }

    // 处理二级导航的切换逻辑
    private void OnSubMenuSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ContentFrame is null || SubMenuColumn is null || SubMenuColumn.Visibility != Visibility.Visible || SettingsSubNavPanel.Visibility != Visibility.Visible)
        {
            return;
        }

        if (sender is ListView listView && listView.SelectedItem is ListViewItem item)
        {
            var key = item.Tag?.ToString() ?? string.Empty;
            string content = item.Content?.ToString() ?? "";

            if (key == "General" || content.Contains("常规"))
            {
                NavigateTo(typeof(SalmonEgg.Presentation.Views.GeneralSettingsPage));
                return;
            }

            if (key == "Appearance" || content.Contains("外观"))
            {
                NavigateTo(typeof(SalmonEgg.Presentation.Views.Settings.AppearanceSettingsPage));
                return;
            }

            if (key == "AgentAcp" || content.Contains("Agent") || content.Contains("ACP") || content.Contains("连接"))
            {
                NavigateTo(typeof(SalmonEgg.Presentation.Views.Settings.AcpConnectionSettingsPage));
                return;
            }

            if (key == "DataStorage" || content.Contains("数据"))
            {
                NavigateTo(typeof(SalmonEgg.Presentation.Views.Settings.DataStorageSettingsPage));
                return;
            }

            if (key == "Shortcuts" || content.Contains("快捷键"))
            {
                NavigateTo(typeof(SalmonEgg.Presentation.Views.Settings.ShortcutsSettingsPage));
                return;
            }

            if (key == "Diagnostics" || content.Contains("诊断"))
            {
                NavigateTo(typeof(SalmonEgg.Presentation.Views.Settings.DiagnosticsSettingsPage));
                return;
            }

            if (key == "About" || content.Contains("关于"))
            {
                NavigateTo(typeof(SalmonEgg.Presentation.Views.Settings.AboutPage));
                return;
            }
        }
    }

    private void OnSubMenuResizerPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (SubMenuColumn is null || SubMenuResizer is null)
        {
            return;
        }

        _isResizingSubMenu = true;
        _subMenuResizeStartX = e.GetCurrentPoint(this).Position.X;
        _subMenuResizeStartWidth = SubMenuColumn.Width;

        SubMenuResizer.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnSubMenuResizerPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizingSubMenu || SubMenuColumn is null)
        {
            return;
        }

        var currentX = e.GetCurrentPoint(this).Position.X;
        var delta = currentX - _subMenuResizeStartX;

        var newWidth = _subMenuResizeStartWidth + delta;
        if (newWidth < SubMenuMinWidth)
        {
            newWidth = SubMenuMinWidth;
        }
        else if (newWidth > SubMenuMaxWidth)
        {
            newWidth = SubMenuMaxWidth;
        }

        SubMenuColumn.Width = newWidth;
        e.Handled = true;
    }

    private void OnSubMenuResizerPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        EndSubMenuResize(e.Pointer);
        e.Handled = true;
    }

    private void OnSubMenuResizerPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        EndSubMenuResize(e.Pointer);
    }

    private void EndSubMenuResize(Pointer pointer)
    {
        if (!_isResizingSubMenu || SubMenuResizer is null)
        {
            return;
        }

        _isResizingSubMenu = false;
        SubMenuResizer.ReleasePointerCapture(pointer);
    }

    private void OnRightPanelButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton button || RightPanelColumn is null)
        {
            return;
        }

        var key = button.Tag?.ToString() ?? string.Empty;
        if (RightPanelColumn.Visibility == Visibility.Visible && _activeRightPanel == key)
        {
            CloseRightPanel();
            return;
        }

        OpenRightPanel(key);
    }

    private void OpenRightPanel(string key)
    {
        if (RightPanelColumn is null || RightPanelTitle is null)
        {
            return;
        }

        _activeRightPanel = key;
        RightPanelColumn.Visibility = Visibility.Visible;
        var baseWidth = double.IsNaN(RightPanelColumn.Width) || RightPanelColumn.Width <= 0 ? 320 : RightPanelColumn.Width;
        RightPanelColumn.Width = Math.Clamp(baseWidth, RightPanelMinWidth, RightPanelMaxWidth);

        RightPanelTitle.Text = key switch
        {
            "Diff" => "Diff",
            "Todo" => "Todo",
            "Files" => "Files",
            _ => "Panel"
        };

        DiffPanel.Visibility = key == "Diff" ? Visibility.Visible : Visibility.Collapsed;
        TodoPanel.Visibility = key == "Todo" ? Visibility.Visible : Visibility.Collapsed;
        FilesPanel.Visibility = key == "Files" ? Visibility.Visible : Visibility.Collapsed;

        DiffPanelButton.IsChecked = key == "Diff";
        TodoPanelButton.IsChecked = key == "Todo";
        FilesPanelButton.IsChecked = key == "Files";
    }

    private void CloseRightPanel()
    {
        if (RightPanelColumn is null)
        {
            return;
        }

        _activeRightPanel = null;
        RightPanelColumn.Visibility = Visibility.Collapsed;

        DiffPanel.Visibility = Visibility.Collapsed;
        TodoPanel.Visibility = Visibility.Collapsed;
        FilesPanel.Visibility = Visibility.Collapsed;

        DiffPanelButton.IsChecked = false;
        TodoPanelButton.IsChecked = false;
        FilesPanelButton.IsChecked = false;
    }

    private void UpdateRightPanelAvailability(bool isChat)
    {
        DiffPanelButton.IsEnabled = isChat;
        TodoPanelButton.IsEnabled = isChat;
        FilesPanelButton.IsEnabled = isChat;

        if (!isChat)
        {
            CloseRightPanel();
        }
    }

    private void OnRightPanelResizerPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (RightPanelColumn is null || RightPanelResizer is null || RightPanelColumn.Visibility != Visibility.Visible)
        {
            return;
        }

        _isResizingRightPanel = true;
        _rightPanelResizeStartX = e.GetCurrentPoint(this).Position.X;
        _rightPanelResizeStartWidth = RightPanelColumn.Width;

        RightPanelResizer.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnRightPanelResizerPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizingRightPanel || RightPanelColumn is null)
        {
            return;
        }

        var currentX = e.GetCurrentPoint(this).Position.X;
        var delta = currentX - _rightPanelResizeStartX;
        var newWidth = _rightPanelResizeStartWidth - delta;

        if (newWidth < RightPanelMinWidth)
        {
            newWidth = RightPanelMinWidth;
        }
        else if (newWidth > RightPanelMaxWidth)
        {
            newWidth = RightPanelMaxWidth;
        }

        RightPanelColumn.Width = newWidth;
        e.Handled = true;
    }

    private void OnRightPanelResizerPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        EndRightPanelResize(e.Pointer);
        e.Handled = true;
    }

    private void OnRightPanelResizerPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        EndRightPanelResize(e.Pointer);
    }

    private void EndRightPanelResize(Pointer pointer)
    {
        if (!_isResizingRightPanel || RightPanelResizer is null)
        {
            return;
        }

        _isResizingRightPanel = false;
        RightPanelResizer.ReleasePointerCapture(pointer);
    }

    private void OnMainPageLoaded(object sender, RoutedEventArgs e)
    {
        ConfigureTitleBar();
    }

    private void OnTitleBarLoaded(object sender, RoutedEventArgs e)
    {
#if __SKIA__
        UpdateTitleBarDragRegions();
#endif
    }

    private void OnTitleBarSizeChanged(object sender, SizeChangedEventArgs e)
    {
#if __SKIA__
        UpdateTitleBarDragRegions();
#endif
    }

    private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
    {
        if (TitleBarBackButton != null)
        {
            TitleBarBackButton.IsEnabled = ContentFrame.CanGoBack;
        }
    }

    private void OnTitleBarBackClick(object sender, RoutedEventArgs e)
    {
        if (ContentFrame?.CanGoBack == true)
        {
            ContentFrame.GoBack();
        }
    }

    private void OnToggleLeftNavClick(object sender, RoutedEventArgs e)
    {
        _isLeftNavCollapsed = !_isLeftNavCollapsed;
        ApplyLeftNavVisibility();
    }

    private void ApplyLeftNavVisibility()
    {
        if (MainRailColumn == null || SubMenuColumn == null)
        {
            return;
        }

        if (_isLeftNavCollapsed)
        {
            MainRailColumn.Visibility = Visibility.Collapsed;
            SubMenuColumn.Visibility = Visibility.Collapsed;
            return;
        }

        MainRailColumn.Visibility = Visibility.Visible;
        RestoreSubMenuVisibility();
    }

    private void RestoreSubMenuVisibility()
    {
        if (SubMenuColumn == null || ChatSubNavPanel == null || SettingsSubNavPanel == null)
        {
            return;
        }

        if (BottomRailNav?.SelectedItem == SettingsNavItem)
        {
            SubMenuColumn.Visibility = Visibility.Visible;
            ChatSubNavPanel.Visibility = Visibility.Collapsed;
            SettingsSubNavPanel.Visibility = Visibility.Visible;
            return;
        }

        if (MainRailNav?.SelectedItem == ChatNavItem)
        {
            SubMenuColumn.Visibility = Visibility.Visible;
            ChatSubNavPanel.Visibility = Visibility.Visible;
            SettingsSubNavPanel.Visibility = Visibility.Collapsed;
            return;
        }

        SubMenuColumn.Visibility = Visibility.Collapsed;
    }

    private void ConfigureTitleBar()
    {
        var window = App.MainWindowInstance;
        if (window == null || AppTitleBar is null)
        {
            return;
        }

#if __SKIA__
        try
        {
            window.ExtendsContentIntoTitleBar = true;
            window.SetTitleBar(AppTitleBar);
        }
        catch
        {
            return;
        }

        if (AppTitleBarContent != null)
        {
            AppTitleBarContent.Padding = new Thickness(0, 0, 140, 0);
        }
#endif

#if __SKIA__
        // Use custom caption regions for Skia to keep controls interactive.
        UpdateTitleBarDragRegions();
#endif

#if WINDOWS
        try
        {
            window.ExtendsContentIntoTitleBar = true;
            window.SetTitleBar(AppTitleBar);
        }
        catch
        {
            return;
        }

        if (TitleBarLeftButtons != null)
        {
            TitleBarLeftButtons.Visibility = Visibility.Visible;
        }

        if (TitleBarBackButton != null)
        {
            TitleBarBackButton.IsEnabled = ContentFrame.CanGoBack;
        }

        var appWindow = window.AppWindow;
        if (appWindow?.TitleBar == null)
        {
            return;
        }

        _appWindowTitleBar = appWindow.TitleBar;
        _appWindowTitleBar.ExtendsContentIntoTitleBar = true;
        _appWindowTitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        _appWindowTitleBar.BackgroundColor = Colors.Transparent;
        _appWindowTitleBar.InactiveBackgroundColor = Colors.Transparent;
        _appWindowTitleBar.ButtonBackgroundColor = Colors.Transparent;
        _appWindowTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        _appWindowTitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
        _appWindowTitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
        UpdateTitleBarInsets();

        window.Activated += OnMainWindowActivated;
        window.SizeChanged += OnMainWindowSizeChanged;
#endif
    }

#if __SKIA__
    private void UpdateTitleBarDragRegions()
    {
        if (AppTitleBar == null || TitleBarDragLeft == null || TitleBarDragRight == null)
        {
            return;
        }

        var window = App.MainWindowInstance;
        if (window == null)
        {
            return;
        }

        try
        {
            _hasNonClientDragRegions = false;
            var appWindowProp = window.GetType().GetProperty("AppWindow");
            var appWindow = appWindowProp?.GetValue(window);
            var windowIdProp = appWindow?.GetType().GetProperty("Id");
            var windowId = windowIdProp?.GetValue(appWindow);
            if (windowId == null)
            {
                return;
            }

            var inputSource = InputNonClientPointerSource.GetForWindowId((Microsoft.UI.WindowId)windowId);
            var rects = new List<RectInt32>();

            var leftRect = GetDragRect(TitleBarDragLeft);
            if (leftRect.Width > 0 && leftRect.Height > 0)
            {
                rects.Add(leftRect);
            }

            var rightRect = GetDragRect(TitleBarDragRight);
            if (rightRect.Width > 0 && rightRect.Height > 0)
            {
                rects.Add(rightRect);
            }

            inputSource.SetRegionRects(NonClientRegionKind.Caption, rects.ToArray());
            _hasNonClientDragRegions = rects.Count > 0;
        }
        catch
        {
            _hasNonClientDragRegions = false;
        }
    }

    private RectInt32 GetDragRect(FrameworkElement element)
    {
        var root = App.MainWindowInstance?.Content as UIElement;
        var scale = AppTitleBar?.XamlRoot?.RasterizationScale ?? 1.0;

        var transform = root != null
            ? element.TransformToVisual(root)
            : element.TransformToVisual(AppTitleBar);
        var origin = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

        var x = (int)Math.Round(origin.X * scale);
        var y = (int)Math.Round(origin.Y * scale);
        var width = (int)Math.Round(element.ActualWidth * scale);
        var height = (int)Math.Round(element.ActualHeight * scale);

        return new RectInt32(x, y, width, height);
    }

#endif

    private void OnTitleBarDragPointerPressed(object sender, PointerRoutedEventArgs e)
    {
#if __SKIA__
        if (_hasNonClientDragRegions)
        {
            return;
        }

        var window = App.MainWindowInstance;
        if (window == null)
        {
            return;
        }

        try
        {
            var method = window.GetType().GetMethod("TryDragMove");
            method?.Invoke(window, null);
            e.Handled = true;
        }
        catch
        {
        }
#endif
    }

#if WINDOWS
    private void OnMainWindowActivated(object sender, WindowActivatedEventArgs e)
    {
        UpdateTitleBarInsets();
    }

    private void OnMainWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        UpdateTitleBarInsets();
    }

    private void UpdateTitleBarInsets()
    {
        if (_appWindowTitleBar == null || AppTitleBar is null)
        {
            return;
        }

        AppTitleBar.Padding = new Thickness(_appWindowTitleBar.LeftInset, 0, _appWindowTitleBar.RightInset, 0);
        if (_appWindowTitleBar.Height > 0)
        {
            AppTitleBar.Height = _appWindowTitleBar.Height;
        }
    }
#endif
}
public sealed partial class MainPage
{
    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        Preferences.PropertyChanged -= OnPreferencesPropertyChanged;

        Loaded -= OnMainPageLoaded;
        ContentFrame.Navigated -= OnContentFrameNavigated;
        if (AppTitleBar != null)
        {
            AppTitleBar.Loaded -= OnTitleBarLoaded;
            AppTitleBar.SizeChanged -= OnTitleBarSizeChanged;
        }

        if (TitleBarDragLeft != null)
        {
            TitleBarDragLeft.SizeChanged -= OnTitleBarSizeChanged;
        }

        if (TitleBarDragRight != null)
        {
            TitleBarDragRight.SizeChanged -= OnTitleBarSizeChanged;
        }
#if WINDOWS
        if (App.MainWindowInstance != null)
        {
            App.MainWindowInstance.Activated -= OnMainWindowActivated;
            App.MainWindowInstance.SizeChanged -= OnMainWindowSizeChanged;
        }
#endif
    }
}
