using System.ComponentModel;
using Microsoft.UI.Xaml;
#if WINDOWS
using Microsoft.UI.Xaml.Media;
using SalmonEgg.Presentation.Utilities;
#endif
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.Services;

public sealed class WindowBackdropService
{
    private readonly AppPreferencesViewModel _preferences;
    private readonly HashSet<Window> _windows = [];

    public WindowBackdropService(AppPreferencesViewModel preferences)
    {
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _preferences.PropertyChanged += OnPreferencesPropertyChanged;
    }

    public void Attach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!_windows.Add(window))
        {
            Apply(window);
            return;
        }

        window.Closed += OnWindowClosed;
        Apply(window);
    }

    public void Detach(Window window)
    {
        if (!_windows.Remove(window))
        {
            return;
        }

        window.Closed -= OnWindowClosed;
    }

    private void OnWindowClosed(object sender, WindowEventArgs e)
    {
        if (sender is Window window)
        {
            Detach(window);
        }
    }

    private void OnPreferencesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.PropertyName) &&
            e.PropertyName != nameof(AppPreferencesViewModel.Backdrop))
        {
            return;
        }

        foreach (var window in _windows.ToArray())
        {
            Apply(window);
        }
    }

    private void Apply(Window window)
    {
#if WINDOWS
        var queue = window.DispatcherQueue;
        if (queue != null && !queue.HasThreadAccess)
        {
            _ = queue.TryEnqueue(() => ApplyCore(window));
            return;
        }

        ApplyCore(window);
#endif
    }

#if WINDOWS
    private void ApplyCore(Window window)
    {
        var kind = WindowBackdropPreferenceResolver.Resolve(
            _preferences.Backdrop,
            supportsMica: OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000),
            supportsAcrylic: OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041));

        window.SystemBackdrop = kind switch
        {
            WindowBackdropKind.Mica => new MicaBackdrop(),
            WindowBackdropKind.Acrylic => new DesktopAcrylicBackdrop(),
            _ => null
        };
    }
#endif
}
