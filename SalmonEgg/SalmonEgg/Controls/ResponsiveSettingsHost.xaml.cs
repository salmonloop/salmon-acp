using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SalmonEgg.Controls;

public sealed partial class ResponsiveSettingsHost : UserControl
{
    private bool _isUpdatingColumns;

    public static readonly DependencyProperty ChildProperty =
        DependencyProperty.Register(
            nameof(Child),
            typeof(object),
            typeof(ResponsiveSettingsHost),
            new PropertyMetadata(null));

    public static readonly DependencyProperty MaxContentWidthProperty =
        DependencyProperty.Register(
            nameof(MaxContentWidth),
            typeof(double),
            typeof(ResponsiveSettingsHost),
            new PropertyMetadata(780d, OnMaxContentWidthChanged));

    public static readonly DependencyProperty MinGutterProperty =
        DependencyProperty.Register(
            nameof(MinGutter),
            typeof(double),
            typeof(ResponsiveSettingsHost),
            new PropertyMetadata(24d, OnMaxContentWidthChanged));

    public object? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    public double MaxContentWidth
    {
        get => (double)GetValue(MaxContentWidthProperty);
        set => SetValue(MaxContentWidthProperty, value);
    }

    public double MinGutter
    {
        get => (double)GetValue(MinGutterProperty);
        set => SetValue(MinGutterProperty, value);
    }

    public ResponsiveSettingsHost()
    {
        InitializeComponent();
    }

    private static void OnMaxContentWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResponsiveSettingsHost host)
        {
            host.UpdateColumns(host.LayoutRoot?.ActualWidth ?? host.ActualWidth);
        }
    }

    private void OnRootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateColumns(e.NewSize.Width);
    }

    private void UpdateColumns(double availableWidth)
    {
        if (_isUpdatingColumns || availableWidth <= 0)
        {
            return;
        }

        _isUpdatingColumns = true;
        try
        {
            var max = MaxContentWidth;
            var minGutter = Math.Max(0, MinGutter);

            if (max <= 0)
            {
                ContentColumn.Width = new GridLength(1, GridUnitType.Star);
                LeftGutter.Width = new GridLength(minGutter, GridUnitType.Pixel);
                RightGutter.Width = new GridLength(minGutter, GridUnitType.Pixel);
                return;
            }

            var wideThreshold = max + (minGutter * 2);

            if (availableWidth >= wideThreshold)
            {
                // Wide mode: Content is fixed, gutters take remaining space (1*)
                ContentColumn.Width = new GridLength(max, GridUnitType.Pixel);
                LeftGutter.Width = new GridLength(1, GridUnitType.Star);
                RightGutter.Width = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                // Narrow mode: Content takes remaining space (1*), gutters are fixed to MinGutter
                ContentColumn.Width = new GridLength(1, GridUnitType.Star);
                LeftGutter.Width = new GridLength(minGutter, GridUnitType.Pixel);
                RightGutter.Width = new GridLength(minGutter, GridUnitType.Pixel);
            }
        }
        finally
        {
            _isUpdatingColumns = false;
        }
    }
}
