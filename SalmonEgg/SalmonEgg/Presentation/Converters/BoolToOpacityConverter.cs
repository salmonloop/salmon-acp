using System;
using Microsoft.UI.Xaml.Data;

namespace SalmonEgg.Presentation.Converters;

public sealed class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool flag && flag ? 1.0 : 0.55;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
