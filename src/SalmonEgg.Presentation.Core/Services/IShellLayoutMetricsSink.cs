using System.Threading.Tasks;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;

namespace SalmonEgg.Presentation.Core.Services;

public interface IShellLayoutMetricsSink
{
    ValueTask ReportWindowMetrics(double width, double height, double effectiveWidth, double effectiveHeight);
    ValueTask ReportTitleBarInsets(double left, double right, double height);
    ValueTask ReportNavToggle(string source);
    ValueTask ReportRightPanelMode(RightPanelMode mode);
    ValueTask ReportRightPanelWidth(double width);
    ValueTask ReportLeftNavWidth(double width);
}

public sealed class ShellLayoutMetricsSink : IShellLayoutMetricsSink
{
    private readonly IShellLayoutStore _store;
    public ShellLayoutMetricsSink(IShellLayoutStore store) => _store = store;

    public ValueTask ReportWindowMetrics(double width, double height, double effectiveWidth, double effectiveHeight)
        => _store.Dispatch(new WindowMetricsChanged(width, height, effectiveWidth, effectiveHeight));

    public ValueTask ReportTitleBarInsets(double left, double right, double height)
        => _store.Dispatch(new TitleBarInsetsChanged(left, right, height));

    public ValueTask ReportNavToggle(string source)
        => _store.Dispatch(new NavToggleRequested(source));

    public ValueTask ReportRightPanelMode(RightPanelMode mode)
        => _store.Dispatch(new RightPanelModeChanged(mode));

    public ValueTask ReportRightPanelWidth(double width)
        => _store.Dispatch(new RightPanelResizeRequested(width));

    public ValueTask ReportLeftNavWidth(double width)
        => _store.Dispatch(new LeftNavResizeRequested(width));
}
