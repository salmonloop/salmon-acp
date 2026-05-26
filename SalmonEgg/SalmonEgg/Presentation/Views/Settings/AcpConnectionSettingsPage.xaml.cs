using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SalmonEgg.Presentation.Models;
using SalmonEgg.Presentation.Models.Settings;
using SalmonEgg.Presentation.Core.Services.Input;
using SalmonEgg.Presentation.ViewModels.Settings;
using SalmonEgg.Presentation.Views;

namespace SalmonEgg.Presentation.Views.Settings;

public sealed partial class AcpConnectionSettingsPage : SettingsPageBase, INavigationIntentConsumer
{
    public AcpConnectionSettingsViewModel ViewModel { get; }

    public AcpConnectionSettingsPage()
    {
        ViewModel = App.ServiceProvider.GetRequiredService<AcpConnectionSettingsViewModel>();
        InitializeComponent();
        Loaded += OnLoaded;
        SetSettingsBreadcrumbForSection(SettingsSectionCatalog.AgentAcpKey);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.Profiles.RefreshCommand.ExecuteAsync(null);
    }

    private void OnAddProfileClick(object sender, RoutedEventArgs e)
    {
        Frame?.Navigate(
            typeof(AgentProfileEditorPage),
            new AgentProfileEditorArgs(isEditing: false, profileId: null),
            UiMotionController.Current.CreateNavigationTransitionInfo());
    }

    private void OnEditProfileMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item || item.Tag is not string profileId)
        {
            return;
        }

        Frame?.Navigate(
            typeof(AgentProfileEditorPage),
            new AgentProfileEditorArgs(isEditing: true, profileId: profileId),
            UiMotionController.Current.CreateNavigationTransitionInfo());
    }

    
    private async void OnDeleteProfileMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item || item.Tag is not string profileId)
        {
            return;
        }

        var config = ViewModel.Profiles.Profiles.FirstOrDefault(p => p.Id == profileId);
        if (config != null)
        {
            await ViewModel.Profiles.DeleteCommand.ExecuteAsync(config);
        }
    }

    private async void OnProfileConnectionToggleToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggle || toggle.DataContext is not AgentProfileItemViewModel item)
        {
            return;
        }

        // Ignore programmatic state synchronization; only react to user-initiated toggles.
        if (toggle.IsOn == item.IsConnected)
        {
            return;
        }

        if (!item.ToggleConnectionCommand.CanExecute(null))
        {
            return;
        }

        await item.ToggleConnectionCommand.ExecuteAsync(null);
    }

    public bool TryConsumeNavigationIntent(GamepadNavigationIntent intent)
    {
        return intent switch
        {
            GamepadNavigationIntent.MoveDown => TryMoveFocusWithinPage(moveDown: true),
            GamepadNavigationIntent.MoveUp => TryMoveFocusWithinPage(moveDown: false),
            _ => false
        };
    }

    private bool TryMoveFocusWithinPage(bool moveDown)
    {
        if (XamlRoot is null)
        {
            return false;
        }

        var focusedElement = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
        var current = ResolveFocusedAcpControl(focusedElement);
        if (current is null)
        {
            return false;
        }

        if (ReferenceEquals(current, AcpGlobalEnabledToggle))
        {
            return moveDown
                ? AcpProfilesRefreshButton.Focus(FocusState.Programmatic)
                : false;
        }

        if (ReferenceEquals(current, AcpProfilesRefreshButton))
        {
            return moveDown
                ? AcpProfilesAddButton.Focus(FocusState.Programmatic)
                : AcpGlobalEnabledToggle.Focus(FocusState.Programmatic);
        }

        if (ReferenceEquals(current, AcpProfilesAddButton))
        {
            return moveDown
                ? AcpPathMappingsAddButton.Focus(FocusState.Programmatic)
                : AcpProfilesRefreshButton.Focus(FocusState.Programmatic);
        }

        if (ReferenceEquals(current, AcpPathMappingsAddButton))
        {
            return moveDown
                ? false
                : AcpProfilesAddButton.Focus(FocusState.Programmatic);
        }

        return false;
    }

    private DependencyObject? ResolveFocusedAcpControl(DependencyObject? start)
    {
        var current = start;
        while (current is not null)
        {
            if (ReferenceEquals(current, AcpGlobalEnabledToggle)
                || ReferenceEquals(current, AcpProfilesRefreshButton)
                || ReferenceEquals(current, AcpProfilesAddButton)
                || ReferenceEquals(current, AcpPathMappingsAddButton))
            {
                return current;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
