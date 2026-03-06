using Microsoft.UI.Xaml.Controls;
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
    }
}
