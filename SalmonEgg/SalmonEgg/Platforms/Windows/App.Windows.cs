using System;

namespace SalmonEgg;

public partial class App
{
    partial void ApplyPlatformBackdrops(Microsoft.UI.Xaml.Window window)
    {
        // Native WinUI 3 backdrop. Mica is Windows 11+; fall back to Desktop Acrylic on Windows 10.
        // Avoid hard-failing at startup on older Windows builds.
        try
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                window.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
                BootLog("OnLaunched: MicaBackdrop set");
            }
            else if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            {
                window.SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                BootLog("OnLaunched: DesktopAcrylicBackdrop set");
            }
        }
        catch
        {
            BootLog("OnLaunched: backdrop set failed");
        }
    }
}
