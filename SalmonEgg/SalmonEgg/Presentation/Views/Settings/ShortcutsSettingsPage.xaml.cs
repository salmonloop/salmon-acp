using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.Views.Settings;

public sealed partial class ShortcutsSettingsPage : Page
{
    public ShortcutsSettingsViewModel ViewModel { get; }

    public ShortcutsSettingsPage()
    {
        ViewModel = App.ServiceProvider.GetRequiredService<ShortcutsSettingsViewModel>();
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

    private void OnRestoreSingleClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ShortcutEntryViewModel vm)
        {
            vm.Gesture = vm.DefaultGesture;
        }
    }
}
