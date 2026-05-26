using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SalmonEgg.Presentation.Models.Settings;
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.Views.Settings;

public sealed partial class AppearanceSettingsPage : SalmonEgg.Presentation.Views.SettingsPageBase
{
    public AppPreferencesViewModel Preferences { get; }

    public AppearanceSettingsPage()
    {
        Preferences = App.ServiceProvider.GetRequiredService<AppPreferencesViewModel>();
        InitializeComponent();
        SetSettingsBreadcrumbForSection(SettingsSectionCatalog.AppearanceKey);
    }

    private void OnThemeComboBoxPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.IsDropDownOpen)
        {
            return;
        }

        if (e.Key is Windows.System.VirtualKey.Down or Windows.System.VirtualKey.GamepadDPadDown)
        {
            e.Handled = true;
            _ = DispatcherQueue.TryEnqueue(() => AppearanceAnimationToggle.Focus(FocusState.Programmatic));
        }
    }

    private void OnAnimationToggleKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is Windows.System.VirtualKey.Up or Windows.System.VirtualKey.GamepadDPadUp)
        {
            e.Handled = true;
            _ = DispatcherQueue.TryEnqueue(() => AppearanceThemeComboBox.Focus(FocusState.Programmatic));
        }
        else if (e.Key is Windows.System.VirtualKey.Down or Windows.System.VirtualKey.GamepadDPadDown)
        {
            e.Handled = true;
            _ = DispatcherQueue.TryEnqueue(() => AppearanceBackdropComboBox.Focus(FocusState.Programmatic));
        }
    }

    private void OnBackdropComboBoxPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.IsDropDownOpen)
        {
            return;
        }

        if (e.Key is Windows.System.VirtualKey.Up or Windows.System.VirtualKey.GamepadDPadUp)
        {
            e.Handled = true;
            _ = DispatcherQueue.TryEnqueue(() => AppearanceAnimationToggle.Focus(FocusState.Programmatic));
        }
    }
}
