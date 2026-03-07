using Microsoft.UI.Xaml.Controls;
using UnoAcpClient.Domain.Models;
using UnoAcpClient.Presentation.ViewModels.Chat;

namespace UnoAcpClient.Presentation.Views.Chat
{
    public sealed partial class ChatView : Page
    {
        public ChatViewModel ViewModel { get; }

        public ChatView()
        {
            InitializeComponent();
            // 注意：在 Uno Platform 中使用 x:Bind 时，ViewModel 通常在 XAML 中设置
            // 或者在依赖注入容器中注册并在 App.xaml.cs 中注入
        }

        private void OnTransportTypeChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

                if (ViewModel?.TransportConfig != null)
                {
                    ViewModel.TransportConfig.SelectedTransportType = transportType;
                }
            }
        }
    }
}
