using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using FlaUI.Core.Definitions;
using Xunit.Sdk;

namespace SalmonEgg.GuiTests.Windows;

public sealed class DiagnosticsSettingsSmokeTests
{
    [SkippableFact]
    public void GamepadDiagnosticsMonitor_CanRefreshAndStartFromDiagnosticsSettings()
    {
        GuiTestGate.RequireEnabled();

        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData();
        using var session = WindowsGuiAppSession.LaunchFresh();

        EnsureMainWindowWide(session);
        NavigateToDiagnosticsSettings(session);

        var startButton = FindAndScrollIntoView(session, "Diagnostics.GamepadStart", TimeSpan.FromSeconds(10));
        Assert.False(
            startButton.IsOffscreen,
            $"Gamepad diagnostics start button did not become visible."
            + $"{Environment.NewLine}{appData.ReadBootLogTail()}");
        Assert.True(startButton.IsEnabled, "Gamepad diagnostics start button should be enabled on Windows.");

        session.ActivateElement(FindAndScrollIntoView(session, "Diagnostics.GamepadRefresh", TimeSpan.FromSeconds(5)));
        Assert.NotNull(session.FindByAutomationId("Diagnostics.GamepadStandardCount", TimeSpan.FromSeconds(5)));
        Assert.NotNull(session.FindByAutomationId("Diagnostics.GamepadRawCount", TimeSpan.FromSeconds(5)));
        Assert.NotNull(session.FindByAutomationId("Diagnostics.GamepadActiveInputs", TimeSpan.FromSeconds(5)));
        Assert.NotNull(session.FindByAutomationId("Diagnostics.GamepadRawDetails", TimeSpan.FromSeconds(5)));

        session.ActivateElement(startButton);
        var stopButton = FindAndScrollIntoView(session, "Diagnostics.GamepadStop", TimeSpan.FromSeconds(10));
        Assert.False(
            stopButton.IsOffscreen,
            $"Gamepad diagnostics stop button did not become visible after starting the monitor.{Environment.NewLine}{appData.ReadBootLogTail()}");
        Assert.True(stopButton.IsEnabled, "Gamepad diagnostics stop button should become enabled after starting the monitor.");
    }

    private static void NavigateToDiagnosticsSettings(WindowsGuiAppSession session)
    {
        var settingsItem = session.FindByAutomationId("SettingsItem", TimeSpan.FromSeconds(10));
        session.ActivateElement(settingsItem);
        Thread.Sleep(250);
        session.ClickElement(settingsItem);
        Thread.Sleep(250);

        var diagnosticsItem = session.TryFindByAutomationId("SettingsNav.Diagnostics", TimeSpan.FromSeconds(10));
        if (diagnosticsItem is null)
        {
            throw CreateNavigationFailure(session, "Diagnostics settings entry did not become visible after opening settings.");
        }

        session.ActivateElement(diagnosticsItem);
        if (!session.WaitUntilOnscreen("Diagnostics.GamepadMonitorHeader", TimeSpan.FromSeconds(10)))
        {
            throw CreateNavigationFailure(session, "Gamepad diagnostics monitor header did not become visible.");
        }
    }

    private static XunitException CreateNavigationFailure(WindowsGuiAppSession session, string message)
    {
        var captureRoot = Path.Combine(Path.GetTempPath(), "SalmonEgg.GuiTests");
        Directory.CreateDirectory(captureRoot);
        var capturePath = Path.Combine(captureRoot, $"settings-diagnostics-missing-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");
        session.CaptureMainWindowToFile(capturePath);
        var visibleTexts = string.Join(", ", session.GetVisibleTexts());
        return new XunitException(
            $"{message}{Environment.NewLine}" +
            $"Screenshot: {capturePath}{Environment.NewLine}" +
            $"Visible texts: [{visibleTexts}]");
    }

    private static FlaUI.Core.AutomationElements.AutomationElement FindAndScrollIntoView(
        WindowsGuiAppSession session,
        string automationId,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        FlaUI.Core.AutomationElements.AutomationElement? element = null;
        while (DateTime.UtcNow < deadline)
        {
            element = session.TryFindByAutomationId(automationId, TimeSpan.FromMilliseconds(250));
            if (element is not null)
            {
                break;
            }

            session.ScrollWheel(-120);
            Thread.Sleep(120);
        }

        element ??= session.FindByAutomationId(automationId, TimeSpan.FromMilliseconds(250));
        if (element.Patterns.ScrollItem.IsSupported)
        {
            element.Patterns.ScrollItem.Pattern.ScrollIntoView();
            Thread.Sleep(150);
        }

        return element;
    }

    private static void EnsureMainWindowWide(WindowsGuiAppSession session)
    {
        try
        {
            if (session.MainWindow.Patterns.Window.IsSupported)
            {
                session.MainWindow.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Normal);
            }
        }
        catch
        {
        }

        ResizeMainWindow(width: 1400, height: 900);
    }

    private static void ResizeMainWindow(int width, int height)
    {
        var process = Process.GetProcessesByName("SalmonEgg")
            .OrderByDescending(candidate => candidate.StartTime)
            .First();

        if (NativeMethods.MoveWindow(process.MainWindowHandle, 80, 80, width, height, true))
        {
            return;
        }

        if (NativeMethods.SetWindowPos(process.MainWindowHandle, IntPtr.Zero, 80, 80, width, height, 0))
        {
            return;
        }

        throw new InvalidOperationException("Failed to resize the SalmonEgg window.");
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags);
    }
}
