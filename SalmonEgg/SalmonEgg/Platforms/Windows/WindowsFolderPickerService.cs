using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace SalmonEgg.Presentation.Services;

public sealed class WindowsFolderPickerService : IFolderPickerService
{
    public bool IsSupported => true;

    public async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        var window = App.MainWindowInstance;
        if (window != null)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
