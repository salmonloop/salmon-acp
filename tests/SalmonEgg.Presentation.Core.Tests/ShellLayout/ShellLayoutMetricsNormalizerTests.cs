using SalmonEgg.Presentation.Core.Services;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.ShellLayout;

public sealed class ShellLayoutMetricsNormalizerTests
{
    [Fact]
    public void ResolveEffectiveSize_UsesContentActualSize_WhenAvailable()
    {
        var (effectiveWidth, effectiveHeight) = ShellLayoutMetricsNormalizer.ResolveEffectiveSize(
            reportedWidth: 720,
            reportedHeight: 900,
            contentActualWidth: 320,
            contentActualHeight: 420);

        Assert.Equal(320, effectiveWidth);
        Assert.Equal(420, effectiveHeight);
    }

    [Fact]
    public void ResolveEffectiveSize_FallsBackToReportedSize_WhenContentActualSizeUnavailable()
    {
        var (effectiveWidth, effectiveHeight) = ShellLayoutMetricsNormalizer.ResolveEffectiveSize(
            reportedWidth: 720,
            reportedHeight: 900,
            contentActualWidth: 0,
            contentActualHeight: -1);

        Assert.Equal(720, effectiveWidth);
        Assert.Equal(900, effectiveHeight);
    }
}
