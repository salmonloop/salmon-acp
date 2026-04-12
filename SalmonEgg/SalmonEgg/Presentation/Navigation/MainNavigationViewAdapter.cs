using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SalmonEgg.Application.Common.Shell;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;
using SalmonEgg.Presentation.Core.Services;
using SalmonEgg.Presentation.Models.Navigation;
using SalmonEgg.Presentation.ViewModels.Navigation;

namespace SalmonEgg.Presentation.Navigation;

/// <summary>
/// UI-only adapter that projects navigation selection state onto NavigationView.
/// It absorbs control-specific selection quirks without becoming another state source.
/// </summary>
public sealed class MainNavigationViewAdapter
{
    private readonly NavigationView _navigationView;
    private readonly MainNavigationViewModel _viewModel;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly DispatcherQueue _dispatcherQueue;
    private NavigationViewPanePresentationState _panePresentationState = NavigationViewPanePresentationState.Default;
    private NavigationViewPanePresentationMode _displayMode = NavigationViewPanePresentationMode.Expanded;

    public MainNavigationViewAdapter(
        NavigationView navigationView,
        MainNavigationViewModel viewModel,
        INavigationCoordinator navigationCoordinator,
        DispatcherQueue dispatcherQueue)
    {
        _navigationView = navigationView ?? throw new ArgumentNullException(nameof(navigationView));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
    }

    public async Task<bool> HandleItemInvokedAsync(NavigationViewItemInvokedEventArgs args)
    {
        return await HandleItemInvokedCoreAsync(args).ConfigureAwait(true);
    }

    public void ApplyPaneProjection(bool isPaneOpen)
    {
        if (_navigationView.IsPaneOpen != isPaneOpen)
        {
            _navigationView.IsPaneOpen = isPaneOpen;
        }
    }

    public void SyncProjectExpansion(NavigationViewItemBase? itemContainer, bool isExpanded)
    {
        if (!_navigationView.IsPaneOpen)
        {
            return;
        }

        if ((itemContainer as FrameworkElement)?.DataContext is ProjectNavItemViewModel project)
        {
            _viewModel.SetProjectExpanded(project.ProjectId, isExpanded);
        }
    }

    public void ReapplyProjectExpansionProjection()
    {
        foreach (var project in _viewModel.Items.OfType<ProjectNavItemViewModel>())
        {
            if (_navigationView.ContainerFromMenuItem(project) is not NavigationViewItem container)
            {
                continue;
            }

            var targetExpansion = NavigationViewPanePresentationPolicy.ResolveProjectExpansionProjection(
                _displayMode,
                _navigationView.IsPaneOpen,
                project.IsExpanded);

            if (!targetExpansion.HasValue)
            {
                continue;
            }

            if (container.IsExpanded != targetExpansion.Value)
            {
                container.IsExpanded = targetExpansion.Value;
            }
        }
    }

    public bool HandlePanePresentationChanged(
        bool isPaneOpen,
        bool isDisplayModeChanged,
        NavigationViewPanePresentationMode displayMode,
        bool desiredPaneOpen)
    {
        _displayMode = displayMode;

        var decision = NavigationViewPanePresentationPolicy.Evaluate(
            _panePresentationState,
            isPaneOpen,
            isDisplayModeChanged,
            displayMode,
            desiredPaneOpen);
        _panePresentationState = decision.NextState;

        if (decision.ShouldApplyPaneProjection)
        {
            ApplyPaneProjection(desiredPaneOpen);
        }

        if (decision.ShouldRefreshSelectionProjection
            || decision.ShouldReassertExpandedProjects
            || decision.ShouldReapplyProjectExpansionProjection)
        {
            ScheduleProjectionReplay(
                decision.ShouldRefreshSelectionProjection,
                decision.ShouldReassertExpandedProjects,
                decision.ShouldReapplyProjectExpansionProjection,
                decision.ShouldClearDisplayModeSuppressionAfterReplay);
        }

        return decision.ShouldReportPaneOpenIntent;
    }

    public void TrySyncProjectExpansion(NavigationViewItemBase? itemContainer, bool isExpanded)
    {
        if (!NavigationViewPanePresentationPolicy.ShouldSyncProjectExpansion(_panePresentationState))
        {
            return;
        }

        SyncProjectExpansion(itemContainer, isExpanded);
    }

    private async Task<bool> HandleItemInvokedCoreAsync(NavigationViewItemInvokedEventArgs args)
    {
        if (ReferenceEquals(args.InvokedItemContainer, _navigationView.SettingsItem))
        {
            await _navigationCoordinator.ActivateSettingsAsync("General").ConfigureAwait(true);
            return true;
        }

        if (args.InvokedItemContainer is not NavigationViewItem navItem || navItem.Tag is not string tag)
        {
            return false;
        }

        if (string.Equals(tag, NavItemTag.Start, StringComparison.Ordinal))
        {
            await _navigationCoordinator.ActivateStartAsync().ConfigureAwait(true);
            return true;
        }

        if (string.Equals(tag, NavItemTag.DiscoverSessions, StringComparison.Ordinal))
        {
            await _navigationCoordinator.ActivateDiscoverSessionsAsync().ConfigureAwait(true);
            return true;
        }

        if (NavItemTag.TryParseSession(tag, out var sessionId))
        {
            var sessionProjectId = (args.InvokedItemContainer as FrameworkElement)?.DataContext is SessionNavItemViewModel sessionItem
                ? sessionItem.ProjectId
                : _viewModel.TryGetProjectIdForSession(sessionId);

            // Never await remote session activation on the NavigationView UI event pipeline.
            // If we await here, the UI thread stays occupied until activation completes,
            // which causes multi-second "click has no response" freezes.
            _ = _navigationCoordinator.ActivateSessionAsync(sessionId, sessionProjectId);
            return true;
        }

        if (NavItemTag.TryParseProject(tag, out _))
        {
            // Non-leaf project items are not navigation destinations. Let the native
            // NavigationView hierarchy handle expand/collapse without translating the
            // click into a semantic selection change.
            return true;
        }

        if (string.Equals(tag, NavItemTag.AddProject, StringComparison.Ordinal))
        {
            _ = _viewModel.AddProjectItem.AddProjectCommand.ExecuteAsync(null);
            return true;
        }

        if (NavItemTag.TryParseMore(tag, out var moreProjectId))
        {
            _ = _viewModel.ShowAllSessionsForProjectAsync(moreProjectId);
            return true;
        }

        return false;
    }
    private void ScheduleProjectionReplay(
        bool refreshSelectionProjection,
        bool reassertExpandedProjects,
        bool reapplyProjectExpansionProjection,
        bool clearDisplayModeSuppressionAfterReplay)
    {
        _ = _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (refreshSelectionProjection)
            {
                _viewModel.RefreshSelectionProjection();
            }

            if (reassertExpandedProjects)
            {
                _viewModel.ReassertExpandedProjects();
            }

            if (reapplyProjectExpansionProjection)
            {
                ReapplyProjectExpansionProjection();
            }

            if (clearDisplayModeSuppressionAfterReplay
                && _panePresentationState.IsDisplayModeTransitionSuppressed)
            {
                _panePresentationState = NavigationViewPanePresentationState.Default;
            }
        });
    }
}
