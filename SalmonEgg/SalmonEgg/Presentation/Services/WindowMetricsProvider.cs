using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using SalmonEgg.Presentation.Core.Services;

namespace SalmonEgg.Presentation.Services;

public sealed class WindowMetricsProvider
{
    private readonly IShellLayoutMetricsSink _sink;

    public WindowMetricsProvider(IShellLayoutMetricsSink sink)
    {
        _sink = sink;
    }

    private Window? _window;
    private AppWindowTitleBar? _titleBar;
    private FrameworkElement? _contentRoot;

    public void Attach(Window window, AppWindowTitleBar? titleBar)
    {
        Detach();

        _window = window;
        _titleBar = titleBar;
        _contentRoot = _window.Content as FrameworkElement;

        _window.SizeChanged += OnSizeChanged;
        _window.Activated += OnActivated;
        if (_contentRoot != null)
        {
            _contentRoot.SizeChanged += OnContentRootSizeChanged;
        }

        // Initial report
        ReportWindowMetrics(_window.Bounds.Width, _window.Bounds.Height);

        if (_titleBar != null)
        {
            ReportTitleBarInsets(_titleBar);
        }
    }

    public void Detach()
    {
        if (_window != null)
        {
            _window.SizeChanged -= OnSizeChanged;
            _window.Activated -= OnActivated;
        }
        if (_contentRoot != null)
        {
            _contentRoot.SizeChanged -= OnContentRootSizeChanged;
        }

        _window = null;
        _titleBar = null;
        _contentRoot = null;
    }

    private void OnSizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        ReportWindowMetrics(e.Size.Width, e.Size.Height);
    }

    private void OnActivated(object sender, WindowActivatedEventArgs e)
    {
        if (_titleBar != null)
        {
            ReportTitleBarInsets(_titleBar);
        }
    }

    private void OnContentRootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_window is null)
        {
            return;
        }

        ReportWindowMetrics(_window.Bounds.Width, _window.Bounds.Height);
    }

    private void ReportTitleBarInsets(AppWindowTitleBar titleBar)
    {
        var (left, right, height) = GetTitleBarInsets(titleBar);
        _ = _sink.ReportTitleBarInsets(left, right, height);
    }

    private void ReportWindowMetrics(double width, double height)
    {
        var content = _window?.Content as FrameworkElement;
        var contentActualWidth = content?.ActualWidth ?? 0;
        var contentActualHeight = content?.ActualHeight ?? 0;
        var (effectiveWidth, effectiveHeight) = ShellLayoutMetricsNormalizer.ResolveEffectiveSize(
            width,
            height,
            contentActualWidth,
            contentActualHeight);

        _ = _sink.ReportWindowMetrics(width, height, effectiveWidth, effectiveHeight);
    }

    private static (double Left, double Right, double Height) GetTitleBarInsets(AppWindowTitleBar titleBar)
    {
#if WINDOWS
        return (titleBar.LeftInset, titleBar.RightInset, titleBar.Height);
#else
        return (0, 0, titleBar.Height);
#endif
    }
}
