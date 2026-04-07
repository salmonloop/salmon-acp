namespace SalmonEgg.Presentation.Core.Services;

public static class ShellLayoutMetricsNormalizer
{
    public static (double EffectiveWidth, double EffectiveHeight) ResolveEffectiveSize(
        double reportedWidth,
        double reportedHeight,
        double contentActualWidth,
        double contentActualHeight)
    {
        var effectiveWidth = contentActualWidth > 0 ? contentActualWidth : reportedWidth;
        var effectiveHeight = contentActualHeight > 0 ? contentActualHeight : reportedHeight;
        return (effectiveWidth, effectiveHeight);
    }
}
