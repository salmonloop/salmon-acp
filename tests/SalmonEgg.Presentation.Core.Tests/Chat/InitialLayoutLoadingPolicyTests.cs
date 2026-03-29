using SalmonEgg.Presentation.Utilities;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Chat;

public sealed class InitialLayoutLoadingPolicyTests
{
    [Theory]
    [InlineData(true, 3, true, false, true)]
    [InlineData(true, 3, false, false, false)]
    [InlineData(true, 3, true, true, false)]
    [InlineData(true, 0, true, false, false)]
    [InlineData(false, 3, true, false, false)]
    public void ShouldKeepLoading_UsesPendingInitialScrollAsTheFallbackExitGate(
        bool isSessionActive,
        int messageCount,
        bool hasPendingInitialScroll,
        bool lastItemContainerGenerated,
        bool expected)
    {
        var actual = InitialLayoutLoadingPolicy.ShouldKeepLoading(
            isSessionActive,
            messageCount,
            hasPendingInitialScroll,
            lastItemContainerGenerated);

        Assert.Equal(expected, actual);
    }
}
