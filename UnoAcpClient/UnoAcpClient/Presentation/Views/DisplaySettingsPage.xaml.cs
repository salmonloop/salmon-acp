using Microsoft.UI.Xaml.Controls;
using UnoAcpClient.Presentation.ViewModels;

namespace UnoAcpClient.Presentation.Views
{
    public sealed partial class DisplaySettingsPage : Page
    {
        public SettingsViewModel SettingsVM { get; }

        public DisplaySettingsPage()
        {
            this.InitializeComponent();

            // 从全局 DI 容器获取 SettingsViewModel 以保持状态同步
            SettingsVM = App.ServiceProvider.GetRequiredService<SettingsViewModel>();
        }
    }
}
