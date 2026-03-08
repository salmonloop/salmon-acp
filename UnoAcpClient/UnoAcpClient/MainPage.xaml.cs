using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UnoAcpClient.Presentation.ViewModels;
using UnoAcpClient.Presentation.Views;
using UnoAcpClient.Presentation.Views.Chat;

namespace UnoAcpClient;

public sealed partial class MainPage : Page
{
    public SettingsViewModel SettingsVM { get; }

    // 公开暴露导航列表，以便子页面可以触发全局导航切换
    public ListView MainRailNavList => MainRailNav;
    public ListView BottomRailNavList => BottomRailNav;

    public MainPage()
    {
        // 1. 在初始化组件前获取 ViewModel，确保 x:Bind 绑定正常
        SettingsVM = App.ServiceProvider.GetRequiredService<SettingsViewModel>();

        this.InitializeComponent();

        // 2. 监听全局设置变化（如动画开关）
        SettingsVM.PropertyChanged += OnSettingsViewModelPropertyChanged;

        // 3. 初始化动画状态
        UpdateNavigationTransitions();

        // 4. 启动后默认进入对话界面
        ContentFrame.Navigate(typeof(ChatView));
    }

    private void OnSettingsViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsVM.IsAnimationEnabled))
        {
            UpdateNavigationTransitions();
        }
    }

    private void UpdateNavigationTransitions()
    {
#pragma warning disable Uno0001
        // 根据全局设置动态开启或关闭 Frame 的过渡动画
        if (SettingsVM.IsAnimationEnabled)
        {
            ContentFrame.ContentTransitions = new TransitionCollection
            {
                new NavigationThemeTransition { DefaultNavigationTransitionInfo = new EntranceNavigationTransitionInfo() }
            };
        }
        else
        {
            ContentFrame.ContentTransitions = null;
        }
#pragma warning restore Uno0001
    }

    private void OnMainRailNavSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MainRailNav.SelectedItem is ListViewItem item && item == ChatNavItem)
        {
            // 互斥逻辑：选中上方导航时，清除下方导航的选中状态
            BottomRailNav.SelectionChanged -= OnBottomRailNavSelectionChanged;
            BottomRailNav.SelectedIndex = -1;
            BottomRailNav.SelectionChanged += OnBottomRailNavSelectionChanged;

            // 聊天界面不需要二级菜单
            SubMenuColumn.Visibility = Visibility.Collapsed;
            ContentFrame.Navigate(typeof(ChatView), null, GetTransition());
        }
    }

    private void OnBottomRailNavSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BottomRailNav.SelectedItem is ListViewItem item && item == SettingsNavItem)
        {
            // 互斥逻辑：选中下方导航时，清除上方导航的选中状态
            MainRailNav.SelectionChanged -= OnMainRailNavSelectionChanged;
            MainRailNav.SelectedIndex = -1;
            MainRailNav.SelectionChanged += OnMainRailNavSelectionChanged;

            // 展开二级导航栏（设置），并默认加载外观设置
            SubMenuColumn.Visibility = Visibility.Visible;
            ContentFrame.Navigate(typeof(DisplaySettingsPage), null, GetTransition());
        }
    }

    // 处理二级导航的切换逻辑
    private void OnSubMenuSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is ListViewItem item)
        {
            string content = item.Content?.ToString() ?? "";

            if (content.Contains("外观") || content.Contains("常规"))
            {
                ContentFrame.Navigate(typeof(DisplaySettingsPage), null, GetTransition());
            }
            else if (content.Contains("连接状态"))
            {
                // 连接配置已整合至 SettingsPage
                ContentFrame.Navigate(typeof(SettingsPage), null, GetTransition());
            }
        }
    }

    // 辅助方法：根据当前动画设置获取导航过渡信息
    private NavigationTransitionInfo? GetTransition()
    {
#pragma warning disable Uno0001
        return SettingsVM.IsAnimationEnabled
            ? new EntranceNavigationTransitionInfo()
            : (NavigationTransitionInfo)new SuppressNavigationTransitionInfo();
#pragma warning restore Uno0001
    }
}
