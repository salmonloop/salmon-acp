using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnoAcpClient.Domain.Models;
using UnoAcpClient.Presentation.ViewModels;
using UnoAcpClient.Presentation.ViewModels.Chat;

namespace UnoAcpClient.Presentation.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }
        public ChatViewModel ChatViewModel { get; }

        public SettingsPage()
        {
            this.InitializeComponent();

            // 从全局 DI 容器获取 ViewModel 以保持状态同步
            ViewModel = App.ServiceProvider.GetRequiredService<SettingsViewModel>();
            ChatViewModel = App.ServiceProvider.GetRequiredService<ChatViewModel>();

            this.Loaded += SettingsPage_Loaded;
        }

        private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 加载已保存的配置列表
            await ViewModel.LoadConfigurationsAsync();
        }

        private void OnTransportTypeChanged(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.CommandParameter is string transportTypeStr)
            {
                var transportType = transportTypeStr switch
                {
                    "Stdio" => TransportType.Stdio,
                    "WebSocket" => TransportType.WebSocket,
                    "HttpSse" => TransportType.HttpSse,
                    _ => TransportType.Stdio
                };

                if (ChatViewModel?.TransportConfig != null)
                {
                    ChatViewModel.TransportConfig.SelectedTransportType = transportType;
                }
            }
        }

        private void AddConfiguration_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddConfiguration();
        }

        private void EditConfiguration_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ServerConfiguration config)
            {
                ViewModel.EditConfiguration(config);
            }
        }

        private async void DeleteConfiguration_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ServerConfiguration config)
            {
                ViewModel.SelectedConfiguration = config;
                await ViewModel.DeleteConfigurationAsync();
            }
        }

        private void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {
            // 此处可以添加全局保存逻辑，目前大部分绑定是双向的
        }
    }
}
