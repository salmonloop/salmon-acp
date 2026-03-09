using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SalmonEgg.Domain.Models;
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.Views.Settings;

public sealed partial class AcpConnectionSettingsPage : Page
{
    public AcpConnectionSettingsViewModel ViewModel { get; }

    public AcpConnectionSettingsPage()
    {
        ViewModel = App.ServiceProvider.GetRequiredService<AcpConnectionSettingsViewModel>();
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.Profiles.RefreshCommand.ExecuteAsync(null);
    }

    private void OnCrumbSettingsClick(object sender, RoutedEventArgs e)
    {
        FindMainPage()?.NavigateToSettingsSubPage("General");
    }

    private MainPage? FindMainPage()
    {
        DependencyObject? current = this;
        while (current != null && current is not MainPage)
        {
            current = VisualTreeHelper.GetParent(current);
        }

        return current as MainPage;
    }

    private void OnAddProfileClick(object sender, RoutedEventArgs e)
    {
        Frame?.Navigate(typeof(AgentProfileEditorPage), new AgentProfileEditorArgs(isEditing: false, profileId: null));
    }

    private void OnEditProfileMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item || item.Tag is not ServerConfiguration config)
        {
            return;
        }

        Frame?.Navigate(typeof(AgentProfileEditorPage), new AgentProfileEditorArgs(isEditing: true, profileId: config.Id));
    }

    private async void OnDeleteProfileMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item || item.Tag is not ServerConfiguration config)
        {
            return;
        }

        await ViewModel.Profiles.DeleteCommand.ExecuteAsync(config);
    }

    private async void OnConnectionToggleToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggle)
        {
            return;
        }

        // Prevent re-entrant toggles while connecting.
        if (ViewModel.Chat.IsConnecting || ViewModel.Chat.IsInitializing)
        {
            return;
        }

        try
        {
            if (toggle.IsOn)
            {
                if (!ViewModel.Chat.IsConnected)
                {
                    await ViewModel.Chat.InitializeAndConnectCommand.ExecuteAsync(null);
                }
            }
            else
            {
                if (ViewModel.Chat.IsConnected)
                {
                    await ViewModel.Chat.DisconnectCommand.ExecuteAsync(null);
                }
            }
        }
        catch
        {
        }
    }
}
