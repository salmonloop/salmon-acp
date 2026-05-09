using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace SalmonEgg.Presentation.Transcript;

public sealed class ItemsRepeaterTranscriptViewportHost : ITranscriptViewportHost
{
    private readonly ScrollViewer _scrollViewer;
    private readonly ItemsRepeater _itemsRepeater;
    private readonly FrameworkElement? _viewportPadding;

    public ItemsRepeaterTranscriptViewportHost(
        ScrollViewer scrollViewer,
        ItemsRepeater itemsRepeater,
        FrameworkElement? viewportPadding = null)
    {
        _scrollViewer = scrollViewer ?? throw new ArgumentNullException(nameof(scrollViewer));
        _itemsRepeater = itemsRepeater ?? throw new ArgumentNullException(nameof(itemsRepeater));
        _viewportPadding = viewportPadding;
        _scrollViewer.ViewChanged += OnViewChanged;
    }

    public event EventHandler? ViewportChanged;

    public bool HasRealizedItem(int index) => index >= 0 && _itemsRepeater.TryGetElement(index) is not null;

    public bool TryGetFirstVisibleIndex(int itemCount, out int index)
    {
        index = -1;
        if (itemCount <= 0)
        {
            return false;
        }

        var firstPartiallyVisibleIndex = -1;
        var fullyVisibleTop = GetViewportPaddingTop();
        for (var candidate = 0; candidate < itemCount; candidate++)
        {
            if (!TryGetItemElement(candidate, out var element))
            {
                continue;
            }

            var relativeOrigin = element.TransformToVisual(_scrollViewer).TransformPoint(default);
            if (relativeOrigin.Y + element.ActualHeight < 0)
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
        if (!TryGetItemElement(index, out var element))
        {
            return false;
        }

        offset = element.TransformToVisual(_scrollViewer).TransformPoint(default).Y;
        return true;
    }

    public void ScrollItemIntoView(int index)
    {
        if (index < 0)
        {
            return;
        }

        var element = _itemsRepeater.TryGetElement(index) ?? _itemsRepeater.GetOrCreateElement(index);
        element?.StartBringIntoView();
    }

    public bool TryGetVerticalOffset(out double verticalOffset)
    {
        verticalOffset = _scrollViewer.VerticalOffset;
        return verticalOffset >= 0;
    }

    public bool TrySetVerticalOffset(double verticalOffset)
    {
        _scrollViewer.ChangeView(null, verticalOffset, null, true);
        return true;
    }

    public bool IsAtBottom(int itemCount, double bottomThreshold, double bottomGeometryTolerance)
    {
        if (itemCount <= 0)
        {
            return true;
        }

        return _scrollViewer.ScrollableHeight - _scrollViewer.VerticalOffset <= GetBottomViewportTolerance(bottomThreshold, bottomGeometryTolerance);
    }

    public bool IsLastItemVisiblyAtBottom(int itemCount, double bottomThreshold, double bottomGeometryTolerance)
    {
        if (itemCount <= 0 || !TryGetItemElement(itemCount - 1, out var element))
        {
            return false;
        }

        Point relativeOrigin = element.TransformToVisual(_scrollViewer).TransformPoint(default);
        var lastItemBottom = relativeOrigin.Y + element.ActualHeight;
        var viewportBottom = _scrollViewer.ViewportHeight - bottomThreshold;
        return lastItemBottom <= viewportBottom + bottomGeometryTolerance;
    }

    public void Dispose()
    {
        _scrollViewer.ViewChanged -= OnViewChanged;
    }

    private bool TryGetItemElement(int index, out FrameworkElement element)
    {
        element = null!;
        if (_itemsRepeater.TryGetElement(index) is not FrameworkElement itemElement)
        {
            return false;
        }

        element = itemElement;
        return true;
    }

    private double GetViewportPaddingTop()
        => _viewportPadding is Border border ? border.Padding.Top : 0d;

    private double GetBottomViewportTolerance(double bottomThreshold, double bottomGeometryTolerance)
    {
        var bottomPadding = _viewportPadding is Border border ? border.Padding.Bottom : 0d;
        return bottomThreshold + bottomGeometryTolerance + bottomPadding;
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        ViewportChanged?.Invoke(this, EventArgs.Empty);
    }
}
