using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.ViewModels.Chat;
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.Views.Chat
{
    public sealed partial class ChatView : Page
    {
        private static bool _autoConnectAttempted;

        public ChatViewModel ViewModel { get; }
        private readonly IConfigurationService _configurationService;
        private readonly AppPreferencesViewModel _preferences;

        public ChatView()
        {
            // 从全局服务容器获取 ViewModel 以确保状态在导航间持久化
            ViewModel = App.ServiceProvider.GetRequiredService<ChatViewModel>();
            _configurationService = App.ServiceProvider.GetRequiredService<IConfigurationService>();
            _preferences = App.ServiceProvider.GetRequiredService<AppPreferencesViewModel>();

            this.InitializeComponent();

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_autoConnectAttempted)
            {
                return;
            }

            _autoConnectAttempted = true;

            // If there is no prior ACP profile, we'll show a prompt component later (not implemented yet).
            var profileId = _preferences.LastSelectedServerId;
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return;
            }

            if (ViewModel.IsConnected || ViewModel.IsConnecting || ViewModel.IsInitializing)
            {
                return;
            }

            ServerConfiguration? config;
            try
            {
                config = await _configurationService.LoadConfigurationAsync(profileId);
            }
            catch
            {
                return;
            }

            if (config == null)
            {
                return;
            }

            ApplyServerConfigurationToTransportConfig(config);

            try
            {
                await ViewModel.ApplyTransportConfigCommand.ExecuteAsync(null);
            }
            catch
            {
            }
        }

        private void ApplyServerConfigurationToTransportConfig(ServerConfiguration config)
        {
            ViewModel.TransportConfig.SelectedTransportType = config.Transport;

            if (config.Transport == TransportType.Stdio)
            {
                ViewModel.TransportConfig.StdioCommand = config.StdioCommand ?? string.Empty;
                ViewModel.TransportConfig.StdioArgs = config.StdioArgs ?? string.Empty;
            }
            else
            {
                ViewModel.TransportConfig.RemoteUrl = config.ServerUrl ?? string.Empty;
            }
        }

        private void OnInputKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (ViewModel.ShowSlashCommands)
            {
                switch (e.Key)
                {
                    case Windows.System.VirtualKey.Tab:
                    case Windows.System.VirtualKey.Enter:
                        if (ViewModel.TryAcceptSelectedSlashCommand())
                        {
                            if (sender is TextBox tb)
                            {
                                tb.SelectionStart = tb.Text?.Length ?? 0;
                            }
                            e.Handled = true;
                            return;
                        }
                        break;
                    case Windows.System.VirtualKey.Up:
                        if (ViewModel.TryMoveSlashSelection(-1))
                        {
                            e.Handled = true;
                            return;
                        }
                        break;
                    case Windows.System.VirtualKey.Down:
                        if (ViewModel.TryMoveSlashSelection(1))
                        {
                            e.Handled = true;
                            return;
                        }
                        break;
                }
            }

            // 支持 Ctrl+Enter 发送消息
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
                if (ctrlPressed)
                {
                    // 在 ChatViewModel 中，发送命令是 SendPromptCommand
                    if (ViewModel.SendPromptCommand != null && ViewModel.SendPromptCommand.CanExecute(null))
                    {
                        ViewModel.SendPromptCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }

        private void OnGoToSettingsClick(object sender, RoutedEventArgs e)
        {
            // 通过视觉树向上查找 MainPage 实例以触发侧边栏切换
            DependencyObject? current = this;
            while (current != null && !(current is MainPage))
            {
                current = VisualTreeHelper.GetParent(current);
            }

            if (current is MainPage mainPage)
            {
                // 切换到底部导航栏的“设置”项
                // 使用公开的 BottomRailNavList 属性以避免权限问题
                mainPage.BottomRailNavList.SelectedIndex = 0;
            }
        }
    }
}
