using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.Views.Settings;

public sealed partial class AppearanceSettingsPage : Page
{
    public AppPreferencesViewModel Preferences { get; }

    public AppearanceSettingsPage()
    {
        Preferences = App.ServiceProvider.GetRequiredService<AppPreferencesViewModel>();
        InitializeComponent();
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
}
