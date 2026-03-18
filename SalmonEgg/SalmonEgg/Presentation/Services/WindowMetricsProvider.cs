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

    public void Attach(Window window, AppWindowTitleBar? titleBar)
    {
        Detach();

        _window = window;
        _titleBar = titleBar;

        _window.SizeChanged += OnSizeChanged;
        _window.Activated += OnActivated;

        // Initial report
        _ = _sink.ReportWindowMetrics(_window.Bounds.Width, _window.Bounds.Height, _window.Bounds.Width, _window.Bounds.Height);

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

        _window = null;
        _titleBar = null;
    }

    private void OnSizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        _ = _sink.ReportWindowMetrics(e.Size.Width, e.Size.Height, e.Size.Width, e.Size.Height);
    }

    private void OnActivated(object sender, WindowActivatedEventArgs e)
    {
        if (_titleBar != null)
        {
            ReportTitleBarInsets(_titleBar);
        }
    }

    private void ReportTitleBarInsets(AppWindowTitleBar titleBar)
    {
        var (left, right, height) = GetTitleBarInsets(titleBar);
        _ = _sink.ReportTitleBarInsets(left, right, height);
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
