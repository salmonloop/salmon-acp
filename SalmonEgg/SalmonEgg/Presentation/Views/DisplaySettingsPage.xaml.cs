using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SalmonEgg.Presentation.ViewModels;

namespace SalmonEgg.Presentation.Views
{
    public sealed partial class DisplaySettingsPage : Page
    {
        public SettingsViewModel SettingsVM { get; }

        public DisplaySettingsPage()
        {
            // 从全局 DI 容器获取 SettingsViewModel 以保持状态同步
            SettingsVM = App.ServiceProvider.GetRequiredService<SettingsViewModel>();

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
