using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using SalmonEgg.Presentation.Core.Mvux.ShellLayout;

namespace SalmonEgg.Presentation.Converters;

public sealed class NavigationPaneDisplayModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is NavigationPaneDisplayMode mode)
        {
            return Convert(mode);
        }

        return NavigationViewPaneDisplayMode.Auto;
    }

    private static NavigationViewPaneDisplayMode Convert(NavigationPaneDisplayMode mode)
        => mode switch
        {
            NavigationPaneDisplayMode.Expanded => NavigationViewPaneDisplayMode.Left,
            NavigationPaneDisplayMode.Compact => NavigationViewPaneDisplayMode.LeftCompact,
            NavigationPaneDisplayMode.Minimal => NavigationViewPaneDisplayMode.LeftMinimal,
            _ => NavigationViewPaneDisplayMode.Auto
        };

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
