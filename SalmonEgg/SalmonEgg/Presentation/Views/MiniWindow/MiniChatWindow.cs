using System;
using System.Collections.Generic;
#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.Graphics;
#endif
using Microsoft.UI.Xaml;

namespace SalmonEgg.Presentation.Views.MiniWindow;

public sealed class MiniChatWindow : Window
{
    private readonly MiniChatView _view;

#if WINDOWS
    private AppWindowTitleBar? _appWindowTitleBar;
    private InputNonClientPointerSource? _titleBarPointerSource;
#endif

    public MiniChatWindow()
    {
        _view = new MiniChatView();
        Content = _view;

        Activated += OnWindowActivated;
        Closed += OnWindowClosed;
        _view.Loaded += OnViewLoaded;
        _view.Unloaded += OnViewUnloaded;
        _view.TitleBarElement.Loaded += OnTitleBarLoaded;
        _view.TitleBarElement.SizeChanged += OnTitleBarSizeChanged;
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
    {
#if WINDOWS
        UpdateTitleBarInsets();
        UpdateTitleBarInteractiveRegions();
#endif
    }

    private void OnWindowClosed(object sender, WindowEventArgs e)
    {
        _view.Loaded -= OnViewLoaded;
        _view.Unloaded -= OnViewUnloaded;
        _view.TitleBarElement.Loaded -= OnTitleBarLoaded;
        _view.TitleBarElement.SizeChanged -= OnTitleBarSizeChanged;
        Activated -= OnWindowActivated;
        Closed -= OnWindowClosed;
    }

    private void OnViewLoaded(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        ConfigureTitleBar();
#endif
    }

    private void OnViewUnloaded(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        _appWindowTitleBar = null;
        _titleBarPointerSource = null;
#endif
    }

    private void OnTitleBarLoaded(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        UpdateTitleBarInsets();
        UpdateTitleBarInteractiveRegions();
#endif
    }

    private void OnTitleBarSizeChanged(object sender, SizeChangedEventArgs e)
    {
#if WINDOWS
        UpdateTitleBarInsets();
        UpdateTitleBarInteractiveRegions();
#endif
    }

#if WINDOWS
    private void ConfigureTitleBar()
    {
        if (_view.TitleBarElement.XamlRoot is null || !AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        try
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(_view.TitleBarElement);
        }
        catch
        {
            return;
        }

        var appWindow = AppWindow;
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

        _titleBarPointerSource = InputNonClientPointerSource.GetForWindowId(appWindow.Id);
        UpdateTitleBarInsets();
        UpdateTitleBarInteractiveRegions();
    }

    private void UpdateTitleBarInsets()
    {
        if (_appWindowTitleBar == null)
        {
            return;
        }

        _view.SetTitleBarInsets(_appWindowTitleBar.LeftInset, _appWindowTitleBar.RightInset);
    }

    private void UpdateTitleBarInteractiveRegions()
    {
        if (_titleBarPointerSource is null || _view.TitleBarElement.XamlRoot is null)
        {
            return;
        }

        var regions = new List<RectInt32>();
        foreach (var element in _view.TitleBarInteractiveElements)
        {
            TryAddInteractiveRegion(element, regions);
        }

        // Let controls inside the caption area keep normal pointer behavior while the rest stays draggable.
        _titleBarPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, regions.ToArray());
    }

    private void TryAddInteractiveRegion(FrameworkElement element, List<RectInt32> regions)
    {
        if (_view.TitleBarElement.XamlRoot is null)
        {
            return;
        }

        if (element.Visibility != Visibility.Visible || element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return;
        }

        var transform = element.TransformToVisual(_view.TitleBarElement);
        var origin = transform.TransformPoint(new Point(0, 0));
        var scale = _view.TitleBarElement.XamlRoot.RasterizationScale;

        regions.Add(new RectInt32(
            (int)Math.Round(origin.X * scale),
            (int)Math.Round(origin.Y * scale),
            Math.Max(1, (int)Math.Round(element.ActualWidth * scale)),
            Math.Max(1, (int)Math.Round(element.ActualHeight * scale))));
    }
#endif
}
