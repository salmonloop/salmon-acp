using SalmonEgg.Application.Common.Shell;
namespace SalmonEgg.Presentation.Core.Tests.Shell;

public sealed class NavigationViewPanePresentationPolicyTests
{
    [Fact]
    public void Evaluate_DisplayModeChanged_ArmsSingleIntentSuppressionWithoutReplay()
    {
        var state = NavigationViewPanePresentationState.Default;

        var decision = NavigationViewPanePresentationPolicy.Evaluate(
            state,
            isPaneOpen: false,
            isDisplayModeChanged: true,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: false);

        Assert.True(decision.NextState.IsDisplayModeTransitionSuppressed);
        Assert.False(decision.NextState.HasConfirmedOverlayPaneOpen);
        Assert.False(decision.ShouldReportPaneOpenIntent);
        Assert.False(decision.ShouldApplyPaneProjection);
        Assert.False(decision.ShouldRefreshSelectionProjection);
        Assert.False(decision.ShouldReassertExpandedProjects);
        Assert.False(decision.ShouldReapplyProjectExpansionProjection);
        Assert.False(decision.ShouldClearDisplayModeSuppressionAfterReplay);
    }

    [Fact]
    public void Evaluate_SuppressedFollowUp_ClearsSuppressionWithoutReportingIntent()
    {
        var state = new NavigationViewPanePresentationState(
            IsDisplayModeTransitionSuppressed: true,
            HasConfirmedOverlayPaneOpen: false);

        var decision = NavigationViewPanePresentationPolicy.Evaluate(
            state,
            isPaneOpen: false,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: true);

        Assert.Equal(NavigationViewPanePresentationState.Default, decision.NextState);
        Assert.False(decision.ShouldReportPaneOpenIntent);
        Assert.False(decision.ShouldApplyPaneProjection);
        Assert.False(decision.ShouldRefreshSelectionProjection);
        Assert.False(decision.ShouldReassertExpandedProjects);
        Assert.False(decision.ShouldReapplyProjectExpansionProjection);
        Assert.False(decision.ShouldClearDisplayModeSuppressionAfterReplay);
    }

    [Fact]
    public void Evaluate_NoStoreDrift_DoesNothing()
    {
        var state = NavigationViewPanePresentationState.Default;

        var decision = NavigationViewPanePresentationPolicy.Evaluate(
            state,
            isPaneOpen: false,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: false);

        Assert.Equal(NavigationViewPanePresentationState.Default, decision.NextState);
        Assert.False(decision.ShouldReportPaneOpenIntent);
        Assert.False(decision.ShouldApplyPaneProjection);
        Assert.False(decision.ShouldRefreshSelectionProjection);
        Assert.False(decision.ShouldReassertExpandedProjects);
        Assert.False(decision.ShouldReapplyProjectExpansionProjection);
        Assert.False(decision.ShouldClearDisplayModeSuppressionAfterReplay);
    }

    [Fact]
    public void Evaluate_ExpandedModeStoreDrift_ReappliesPaneProjection()
    {
        var state = NavigationViewPanePresentationState.Default;

        var decision = NavigationViewPanePresentationPolicy.Evaluate(
            state,
            isPaneOpen: false,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Expanded,
            desiredPaneOpen: true);

        Assert.Equal(NavigationViewPanePresentationState.Default, decision.NextState);
        Assert.False(decision.ShouldReportPaneOpenIntent);
        Assert.True(decision.ShouldApplyPaneProjection);
        Assert.False(decision.ShouldRefreshSelectionProjection);
        Assert.False(decision.ShouldReassertExpandedProjects);
        Assert.False(decision.ShouldReapplyProjectExpansionProjection);
        Assert.False(decision.ShouldClearDisplayModeSuppressionAfterReplay);
    }

    [Fact]
    public void Evaluate_CompactModeOpenDrift_ReportsPaneIntent()
    {
        var state = NavigationViewPanePresentationState.Default;

        var decision = NavigationViewPanePresentationPolicy.Evaluate(
            state,
            isPaneOpen: true,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: false);

        Assert.True(decision.NextState.HasConfirmedOverlayPaneOpen);
        Assert.True(decision.ShouldReportPaneOpenIntent);
        Assert.False(decision.ShouldApplyPaneProjection);
        Assert.False(decision.ShouldRefreshSelectionProjection);
        Assert.False(decision.ShouldReassertExpandedProjects);
        Assert.False(decision.ShouldReapplyProjectExpansionProjection);
    }

    [Fact]
    public void Evaluate_CompactModeCloseDriftWithoutConfirmedOpen_DoesNotReportCloseIntent()
    {
        var state = NavigationViewPanePresentationState.Default;

        var decision = NavigationViewPanePresentationPolicy.Evaluate(
            state,
            isPaneOpen: false,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: true);

        Assert.Equal(state, decision.NextState);
        Assert.False(decision.ShouldReportPaneOpenIntent);
        Assert.False(decision.ShouldApplyPaneProjection);
    }

    [Fact]
    public void Evaluate_CompactModeConfirmedOpenThenCloseDrift_ReportsCloseIntent()
    {
        var openedState = NavigationViewPanePresentationPolicy.Evaluate(
            NavigationViewPanePresentationState.Default,
            isPaneOpen: true,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: true).NextState;

        Assert.True(openedState.HasConfirmedOverlayPaneOpen);

        var closeDecision = NavigationViewPanePresentationPolicy.Evaluate(
            openedState,
            isPaneOpen: false,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: true);

        Assert.True(closeDecision.ShouldReportPaneOpenIntent);
        Assert.False(closeDecision.NextState.HasConfirmedOverlayPaneOpen);
        Assert.False(closeDecision.ShouldApplyPaneProjection);
    }

    [Fact]
    public void Evaluate_CompactSequence_TransientCloseDoesNotOverrideOpenIntentUntilOpenIsConfirmed()
    {
        // 1) expanded -> compact display mode change: arm suppression
        var afterDisplayModeChanged = NavigationViewPanePresentationPolicy.Evaluate(
            NavigationViewPanePresentationState.Default,
            isPaneOpen: false,
            isDisplayModeChanged: true,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: false);

        Assert.True(afterDisplayModeChanged.NextState.IsDisplayModeTransitionSuppressed);

        // 2) follow-up pane event during transition: consume suppression, no intent report
        var afterSuppressedFollowUp = NavigationViewPanePresentationPolicy.Evaluate(
            afterDisplayModeChanged.NextState,
            isPaneOpen: false,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: true);

        Assert.False(afterSuppressedFollowUp.ShouldReportPaneOpenIntent);
        Assert.False(afterSuppressedFollowUp.NextState.IsDisplayModeTransitionSuppressed);

        // 3) immediate transient close while store already wants open: ignore
        var transientClose = NavigationViewPanePresentationPolicy.Evaluate(
            afterSuppressedFollowUp.NextState,
            isPaneOpen: false,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: true);

        Assert.False(transientClose.ShouldReportPaneOpenIntent);
        Assert.False(transientClose.NextState.HasConfirmedOverlayPaneOpen);

        // 4) control reports opened: mark confirmed-open
        var confirmedOpen = NavigationViewPanePresentationPolicy.Evaluate(
            transientClose.NextState,
            isPaneOpen: true,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: true);

        Assert.True(confirmedOpen.NextState.HasConfirmedOverlayPaneOpen);
        Assert.False(confirmedOpen.ShouldReportPaneOpenIntent);

        // 5) user closes after real open: now close intent should be reported
        var afterRealClose = NavigationViewPanePresentationPolicy.Evaluate(
            confirmedOpen.NextState,
            isPaneOpen: false,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Compact,
            desiredPaneOpen: true);

        Assert.True(afterRealClose.ShouldReportPaneOpenIntent);
        Assert.False(afterRealClose.NextState.HasConfirmedOverlayPaneOpen);
    }

    [Fact]
    public void Evaluate_PaneOpenWithoutStoreDrift_DoesNotReplayProjection()
    {
        var state = NavigationViewPanePresentationState.Default;

        var decision = NavigationViewPanePresentationPolicy.Evaluate(
            state,
            isPaneOpen: true,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Expanded,
            desiredPaneOpen: true);

        Assert.Equal(state, decision.NextState);
        Assert.False(decision.ShouldReportPaneOpenIntent);
        Assert.False(decision.ShouldApplyPaneProjection);
        Assert.False(decision.ShouldRefreshSelectionProjection);
        Assert.False(decision.ShouldReassertExpandedProjects);
        Assert.False(decision.ShouldReapplyProjectExpansionProjection);
    }

    [Fact]
    public void ShouldSyncProjectExpansion_AlwaysTrue()
    {
        Assert.True(NavigationViewPanePresentationPolicy.ShouldSyncProjectExpansion(
            new NavigationViewPanePresentationState(
                IsDisplayModeTransitionSuppressed: true,
                HasConfirmedOverlayPaneOpen: false)));
        Assert.True(NavigationViewPanePresentationPolicy.ShouldSyncProjectExpansion(NavigationViewPanePresentationState.Default));
    }

    [Fact]
    public void Evaluate_ExpandedDisplayModeStoreDrift_DoesNotReportCompactPaneIntent()
    {
        var state = NavigationViewPanePresentationState.Default;

        var decision = NavigationViewPanePresentationPolicy.Evaluate(
            state,
            isPaneOpen: false,
            isDisplayModeChanged: false,
            displayMode: NavigationViewPanePresentationMode.Expanded,
            desiredPaneOpen: true);

        Assert.Equal(state, decision.NextState);
        Assert.False(decision.ShouldReportPaneOpenIntent);
    }

    [Fact]
    public void ResolveProjectExpansionProjection_CompactOpen_PreservesProjectExpansion()
    {
        var projection = NavigationViewPanePresentationPolicy.ResolveProjectExpansionProjection(
            displayMode: NavigationViewPanePresentationMode.Compact,
            isPaneOpen: true,
            isProjectExpanded: true);

        Assert.True(projection);
    }

    [Fact]
    public void ResolveProjectExpansionProjection_ClosedPane_DoesNotForceContainerExpansion()
    {
        var projection = NavigationViewPanePresentationPolicy.ResolveProjectExpansionProjection(
            displayMode: NavigationViewPanePresentationMode.Compact,
            isPaneOpen: false,
            isProjectExpanded: true);

        Assert.Null(projection);
    }
}
