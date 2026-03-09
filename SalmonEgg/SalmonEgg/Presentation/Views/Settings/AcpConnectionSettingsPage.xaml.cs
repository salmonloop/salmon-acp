using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SalmonEgg.Domain.Models;
using SalmonEgg.Presentation.ViewModels;
using SalmonEgg.Presentation.ViewModels.Settings;
using SalmonEgg.Presentation.Views;

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

    private async void OnAddProfileClick(object sender, RoutedEventArgs e)
    {
        var editorVm = App.ServiceProvider.GetRequiredService<ConfigurationEditorViewModel>();
        editorVm.LoadNewConfiguration();

        var dialog = new ConfigurationEditorDialog(editorVm);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();

        await ViewModel.Profiles.RefreshCommand.ExecuteAsync(null);
    }

    private async void OnEditProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ServerConfiguration config)
        {
            return;
        }

        var editorVm = App.ServiceProvider.GetRequiredService<ConfigurationEditorViewModel>();
        editorVm.LoadConfiguration(config);

        var dialog = new ConfigurationEditorDialog(editorVm);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();

        await ViewModel.Profiles.RefreshCommand.ExecuteAsync(null);
    }

    private async void OnDeleteProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ServerConfiguration config)
        {
            return;
        }

        await ViewModel.Profiles.DeleteCommand.ExecuteAsync(config);
    }
}

