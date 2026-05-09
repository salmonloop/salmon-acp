using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SalmonEgg.Presentation.Behaviors;
using Windows.Foundation;

namespace SalmonEgg.Presentation.Transcript;

public sealed class ListViewTranscriptViewportHost : ITranscriptViewportHost
{
    private readonly ListView _listView;
    private long _viewportChangeTokenCallback;

    public ListViewTranscriptViewportHost(ListView listView)
    {
        _listView = listView ?? throw new ArgumentNullException(nameof(listView));
        ScrollViewerViewportMonitor.SetIsEnabled(_listView, true);
        _viewportChangeTokenCallback = _listView.RegisterPropertyChangedCallback(
            ScrollViewerViewportMonitor.ViewportChangeTokenProperty,
            OnViewportChanged);
    }

    public event EventHandler? ViewportChanged;

    public bool HasRealizedItem(int index) => index >= 0 && _listView.ContainerFromIndex(index) is not null;

    public bool TryGetFirstVisibleIndex(int itemCount, out int index)
    {
        index = -1;
        if (itemCount <= 0)
        {
            return false;
        }

        var firstPartiallyVisibleIndex = -1;
        var fullyVisibleTop = _listView.Padding.Top;
        for (var candidate = 0; candidate < itemCount; candidate++)
        {
            if (!TryGetContainerAnchor(candidate, out var anchor))
            {
                continue;
            }

            var relativeOrigin = anchor.TransformToVisual(_listView).TransformPoint(default);
            if (relativeOrigin.Y + anchor.ActualHeight < 0)
            {
                continue;
            }

            firstPartiallyVisibleIndex = firstPartiallyVisibleIndex < 0
                ? candidate
                : firstPartiallyVisibleIndex;
            if (relativeOrigin.Y >= fullyVisibleTop)
            {
                index = candidate;
                return true;
            }
        }

        index = firstPartiallyVisibleIndex;
        return index >= 0;
    }

    public bool TryGetRelativeOffsetWithinItem(int index, out double offset)
    {
        offset = 0d;
        if (!TryGetContainerAnchor(index, out var anchor))
        {
            return false;
        }

        offset = anchor.TransformToVisual(_listView).TransformPoint(default).Y;
        return true;
    }

    public void ScrollItemIntoView(object item)
    {
        if (item is null)
        {
            return;
        }

        _listView.ScrollIntoView(item);
    }

    public bool TryGetVerticalOffset(out double verticalOffset)
    {
        verticalOffset = ScrollViewerViewportMonitor.GetVerticalOffset(_listView);
        return verticalOffset >= 0;
    }

    public bool TrySetVerticalOffset(double verticalOffset)
    {
        var scrollViewer = ScrollViewerViewportMonitor.GetAttachedScrollViewer(_listView);
        if (scrollViewer is null)
        {
            return false;
        }

        scrollViewer.ChangeView(null, verticalOffset, null, true);
        return true;
    }

    public bool IsAtBottom(int itemCount, double bottomThreshold, double bottomGeometryTolerance)
    {
        if (itemCount <= 0)
        {
            return true;
        }

        var monitoredScrollableHeight = ScrollViewerViewportMonitor.GetScrollableHeight(_listView);
        if (monitoredScrollableHeight >= 0)
        {
            var monitoredVerticalOffset = ScrollViewerViewportMonitor.GetVerticalOffset(_listView);
            return monitoredScrollableHeight - monitoredVerticalOffset <= GetBottomViewportTolerance(bottomThreshold, bottomGeometryTolerance);
        }

        return IsLastItemVisiblyAtBottom(itemCount, bottomThreshold, bottomGeometryTolerance);
    }

    public bool IsLastItemVisiblyAtBottom(int itemCount, double bottomThreshold, double bottomGeometryTolerance)
    {
        if (itemCount <= 0 || !TryGetContainerAnchor(itemCount - 1, out var anchor))
        {
            return false;
        }

        Point relativeOrigin = anchor.TransformToVisual(_listView).TransformPoint(default);
        var lastItemBottom = relativeOrigin.Y + anchor.ActualHeight;
        var viewportBottom = _listView.ActualHeight - bottomThreshold;
        return lastItemBottom <= viewportBottom + bottomGeometryTolerance;
    }

    public void Dispose()
    {
        if (_viewportChangeTokenCallback == 0)
        {
            return;
        }

        _listView.UnregisterPropertyChangedCallback(
            ScrollViewerViewportMonitor.ViewportChangeTokenProperty,
            _viewportChangeTokenCallback);
        _viewportChangeTokenCallback = 0;
        ScrollViewerViewportMonitor.SetIsEnabled(_listView, false);
    }

    private bool TryGetContainerAnchor(int index, out FrameworkElement anchor)
    {
        anchor = null!;
        if (_listView.ContainerFromIndex(index) is not ListViewItem container)
        {
            return false;
        }

        anchor = container.ContentTemplateRoot as FrameworkElement ?? container;
        return true;
    }

    private double GetBottomViewportTolerance(double bottomThreshold, double bottomGeometryTolerance)
        => bottomThreshold + bottomGeometryTolerance + _listView.Padding.Bottom;

    private void OnViewportChanged(DependencyObject sender, DependencyProperty dp)
    {
        ViewportChanged?.Invoke(this, EventArgs.Empty);
    }
}
