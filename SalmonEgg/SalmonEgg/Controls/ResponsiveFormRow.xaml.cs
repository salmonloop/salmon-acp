using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SalmonEgg.Controls;

public sealed partial class ResponsiveFormRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(object),
            typeof(ResponsiveFormRow),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(object),
            typeof(ResponsiveFormRow),
            new PropertyMetadata(null));

    public object? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public ResponsiveFormRow()
    {
        InitializeComponent();
    }
}
