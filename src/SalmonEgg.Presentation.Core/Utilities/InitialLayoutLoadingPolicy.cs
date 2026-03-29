namespace SalmonEgg.Presentation.Utilities;

public static class InitialLayoutLoadingPolicy
{
    public static bool ShouldKeepLoading(
        bool isSessionActive,
        int messageCount,
        bool hasPendingInitialScroll,
        bool lastItemContainerGenerated)
    {
        if (!isSessionActive || messageCount <= 0)
        {
            return false;
        }

        if (lastItemContainerGenerated)
        {
            return false;
        }

        return hasPendingInitialScroll;
    }
}
