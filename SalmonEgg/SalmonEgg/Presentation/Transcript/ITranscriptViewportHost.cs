namespace SalmonEgg.Presentation.Transcript;

public interface ITranscriptViewportHost : IDisposable
{
    event EventHandler? ViewportChanged;

    bool HasRealizedItem(int index);

    bool TryGetFirstVisibleIndex(int itemCount, out int index);

    bool TryGetRelativeOffsetWithinItem(int index, out double offset);

    void ScrollItemIntoView(int index);

    bool TryGetVerticalOffset(out double verticalOffset);

    bool TrySetVerticalOffset(double verticalOffset);

    bool IsAtBottom(int itemCount, double bottomThreshold, double bottomGeometryTolerance);

    bool IsLastItemVisiblyAtBottom(int itemCount, double bottomThreshold, double bottomGeometryTolerance);
}
