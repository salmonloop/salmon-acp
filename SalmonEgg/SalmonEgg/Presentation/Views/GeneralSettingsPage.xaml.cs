using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.Views
{
    public sealed partial class GeneralSettingsPage : Page
    {
        public AppPreferencesViewModel Preferences { get; }

        public GeneralSettingsPage()
        {
            Preferences = App.ServiceProvider.GetRequiredService<AppPreferencesViewModel>();
            this.InitializeComponent();
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
}
