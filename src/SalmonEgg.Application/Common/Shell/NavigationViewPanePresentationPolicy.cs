namespace SalmonEgg.Application.Common.Shell;

public enum NavigationViewPanePresentationMode
{
    Expanded,
    Compact,
    Minimal
}

public readonly record struct NavigationViewPanePresentationState(
    bool IsDisplayModeTransitionSuppressed,
    bool HasConfirmedOverlayPaneOpen)
{
    public static NavigationViewPanePresentationState Default { get; } = new(false, false);
}

public readonly record struct NavigationViewPanePresentationDecision(
    NavigationViewPanePresentationState NextState,
    bool ShouldReportPaneOpenIntent,
    bool ShouldApplyPaneProjection,
    bool ShouldRefreshSelectionProjection,
    bool ShouldReassertExpandedProjects,
    bool ShouldReapplyProjectExpansionProjection,
    bool ShouldClearDisplayModeSuppressionAfterReplay);

public static class NavigationViewPanePresentationPolicy
{
    public static bool ShouldSyncProjectExpansion(NavigationViewPanePresentationState state)
        => true;

    public static bool? ResolveProjectExpansionProjection(
        NavigationViewPanePresentationMode displayMode,
        bool isPaneOpen,
        bool isProjectExpanded)
    {
        // In native compact/minimal presentations, the pane/flyout decides what is currently
        // visible. We preserve the user's project expansion intent whenever the pane is open,
        // and avoid forcing container state while the pane is closed.
        if (!isPaneOpen)
        {
            return null;
        }

        return isProjectExpanded;
    }

    public static NavigationViewPanePresentationDecision Evaluate(
        NavigationViewPanePresentationState state,
        bool isPaneOpen,
        bool isDisplayModeChanged,
        NavigationViewPanePresentationMode displayMode,
        bool desiredPaneOpen)
    {
        var nextState = state;
        var shouldReportPaneOpenIntent = false;
        var shouldApplyPaneProjection = false;
        var shouldRefreshSelectionProjection = false;
        var shouldReassertExpandedProjects = false;
        var shouldReapplyProjectExpansionProjection = false;
        var shouldClearDisplayModeSuppressionAfterReplay = false;
        var hasStoreDrift = desiredPaneOpen != isPaneOpen;
        var isExpandedMode = displayMode == NavigationViewPanePresentationMode.Expanded;

        // DisplayModeChanged may produce transient pane events that are not user intent.
        // Suppress one follow-up pane intent report to keep store intent stable.
        if (isDisplayModeChanged)
        {
            nextState = new NavigationViewPanePresentationState(
                IsDisplayModeTransitionSuppressed: true,
                HasConfirmedOverlayPaneOpen: false);
            return new NavigationViewPanePresentationDecision(
                nextState,
                shouldReportPaneOpenIntent,
                shouldApplyPaneProjection,
                shouldRefreshSelectionProjection,
                shouldReassertExpandedProjects,
                shouldReapplyProjectExpansionProjection,
                shouldClearDisplayModeSuppressionAfterReplay);
        }

        if (state.IsDisplayModeTransitionSuppressed)
        {
            nextState = state with { IsDisplayModeTransitionSuppressed = false };
            return new NavigationViewPanePresentationDecision(
                nextState,
                shouldReportPaneOpenIntent,
                shouldApplyPaneProjection,
                shouldRefreshSelectionProjection,
                shouldReassertExpandedProjects,
                shouldReapplyProjectExpansionProjection,
                shouldClearDisplayModeSuppressionAfterReplay);
        }

        // Keep pane open state aligned with layout SSOT only in expanded mode.
        if (isExpandedMode && hasStoreDrift)
        {
            shouldApplyPaneProjection = true;
        }

        if (isExpandedMode)
        {
            nextState = nextState with { HasConfirmedOverlayPaneOpen = false };
        }
        else
        {
            if (isPaneOpen)
            {
                nextState = nextState with { HasConfirmedOverlayPaneOpen = true };
            }

            // In compact/minimal, accept "closed" as user intent only after we've
            // confirmed the overlay pane reached open state in this display-mode cycle.
            if (hasStoreDrift)
            {
                if (!desiredPaneOpen && isPaneOpen)
                {
                    shouldReportPaneOpenIntent = true;
                    nextState = nextState with { HasConfirmedOverlayPaneOpen = true };
                }
                else if (desiredPaneOpen && !isPaneOpen && state.HasConfirmedOverlayPaneOpen)
                {
                    shouldReportPaneOpenIntent = true;
                    nextState = nextState with { HasConfirmedOverlayPaneOpen = false };
                }
            }
        }

        return new NavigationViewPanePresentationDecision(
            nextState,
            shouldReportPaneOpenIntent,
            shouldApplyPaneProjection,
            shouldRefreshSelectionProjection,
            shouldReassertExpandedProjects,
            shouldReapplyProjectExpansionProjection,
            shouldClearDisplayModeSuppressionAfterReplay);
    }
}
