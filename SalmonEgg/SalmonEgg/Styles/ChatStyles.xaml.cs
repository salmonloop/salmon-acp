using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace SalmonEgg.Styles;

public sealed partial class ChatStyles : ResourceDictionary
{
    public ChatStyles()
    {
        InitializeComponent();
    }

    public void OnCopyMessageClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { CommandParameter: string text } || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        TryCopyText(text);
    }

    private static void TryCopyText(string text)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
        }
        catch
        {
            // Clipboard availability can vary by host/platform; ignore to keep UI stable.
        }
    }
}
