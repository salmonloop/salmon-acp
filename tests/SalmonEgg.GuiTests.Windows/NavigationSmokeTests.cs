using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using FlaUI.Core.Definitions;
using FlaUI.Core.AutomationElements;

namespace SalmonEgg.GuiTests.Windows;

public sealed class NavigationSmokeTests
{
    [SkippableFact]
    public void Launch_WithSeededData_ShowsMainNav_AndStartIsSelected()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData();
        using var session = WindowsGuiAppSession.LaunchFresh();

        var mainNav = session.FindByAutomationId("MainNavView");
        var startItem = session.FindByAutomationId("MainNav.Start");
        var startTitle = session.FindByAutomationId("StartView.Title");

        Assert.NotNull(mainNav);
        Assert.NotNull(startTitle);

        var selectionItem = startItem.Patterns.SelectionItem.Pattern;
        var launchSnapshot = string.Join(
            "; ",
            $"StartView={session.TryFindByAutomationId("StartView.Title", TimeSpan.FromSeconds(2)) is not null}",
            $"ChatHeader={session.TryFindByAutomationId("ChatView.CurrentSessionNameButton", TimeSpan.FromSeconds(2)) is not null}",
            $"StartSelected={session.TryGetIsSelected("MainNav.Start")}",
            $"Session01Selected={session.TryGetIsSelected("MainNav.Session.gui-session-01")}");
        Assert.True(
            selectionItem.IsSelected.Value,
            $"Launch state mismatch. {launchSnapshot}{Environment.NewLine}{appData.ReadBootLogTail()}");
    }

    [SkippableFact]
    public void ProjectInvoke_DoesNotChangeSelectionOrContent()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData();
        using var session = WindowsGuiAppSession.LaunchFresh();

        session.ActivateElement(session.FindByAutomationId("MainNav.Project.project-1"));

        var startItem = session.FindByAutomationId("MainNav.Start");
        var startTitle = session.FindByAutomationId("StartView.Title");
        var selectionItem = startItem.Patterns.SelectionItem.Pattern;

        Assert.NotNull(startTitle);
        Assert.True(selectionItem.IsSelected.Value);
    }

    [SkippableFact]
    public void SelectSeededSession_UpdatesNavAndChatHeader()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData();
        using var session = WindowsGuiAppSession.LaunchFresh();

        var sessionItem = session.FindByAutomationId("MainNav.Session.gui-session-01");

        session.ActivateElement(sessionItem);

        var chatHeader = session.FindByAutomationId("ChatView.CurrentSessionNameButton", TimeSpan.FromSeconds(10));
        var selectionItem = sessionItem.Patterns.SelectionItem.Pattern;

        Assert.NotNull(chatHeader);
        Assert.Contains("GUI Session 01", chatHeader.Name, StringComparison.Ordinal);
        Assert.True(selectionItem.IsSelected.Value);
    }

    [SkippableFact]
    public void TitleBarPanelButtons_Toggle_ChangesBottomPanelState()
    {
        using var _ = GuiAppDataScope.CreateDeterministicLeftNavData();
        using var session = WindowsGuiAppSession.LaunchFresh();

        var sessionItem = session.FindByAutomationId("MainNav.Session.gui-session-01");
        session.ActivateElement(sessionItem);
        session.FindByAutomationId("ChatView.CurrentSessionNameButton", TimeSpan.FromSeconds(10));

        var bottomPanelButton = session.FindByAutomationId("TitleBar.BottomPanel");
        Skip.IfNot(bottomPanelButton.Patterns.Toggle.IsSupported, "TitleBar.BottomPanel does not expose TogglePattern in current UIA backend.");

        var before = bottomPanelButton.Patterns.Toggle.Pattern.ToggleState.Value;
        bottomPanelButton.Patterns.Toggle.Pattern.Toggle();
        Thread.Sleep(120);

        var after = session.FindByAutomationId("TitleBar.BottomPanel").Patterns.Toggle.Pattern.ToggleState.Value;
        Assert.NotEqual(before, after);
    }

    [SkippableFact]
    public void MoreSessionsDialog_SelectsOverflowSession_AndUpdatesChatHeader()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData(sessionCount: 21);
        using var session = WindowsGuiAppSession.LaunchFresh();

        session.ActivateElement(session.FindByAutomationId("MainNav.More.project-1"));

        var dialog = session.FindByAutomationId("SessionsDialog", TimeSpan.FromSeconds(10));
        var dialogSession = session.FindFirstDescendantByControlType(dialog, ControlType.ListItem, TimeSpan.FromSeconds(10));

        Assert.NotNull(dialogSession);
        session.ActivateElement(dialogSession);

        var chatHeader = session.FindByAutomationId("ChatView.CurrentSessionNameButton", TimeSpan.FromSeconds(10));

        Assert.NotNull(dialog);
        Assert.False(string.IsNullOrWhiteSpace(chatHeader.Name));
    }

    [SkippableFact]
    public void CompactMode_AddProject_HidesExpandedLabel_AndStaysBetweenStartAndProjects()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData();
        using var session = WindowsGuiAppSession.LaunchFresh();

        ResizeMainWindow(width: 800, height: 900);
        Thread.Sleep(1500);

        var startItem = session.FindByAutomationId("MainNav.Start");
        var addProject = session.FindByAutomationId("MainNav.AddProject");
        var firstProject = session.FindByAutomationId("MainNav.Project.project-1");
        var captureRoot = Path.Combine(Path.GetTempPath(), "SalmonEgg.GuiTests");
        Directory.CreateDirectory(captureRoot);
        var screenshotPath = Path.Combine(captureRoot, "nav-compact-main.png");
        session.MainWindow.CaptureToFile(screenshotPath);

        var startRect = startItem.BoundingRectangle;
        var addRect = addProject.BoundingRectangle;
        var projectRect = firstProject.BoundingRectangle;
        var startCenterY = startRect.Y + (startRect.Height / 2d);
        var addCenterY = addRect.Y + (addRect.Height / 2d);
        var projectCenterY = projectRect.Y + (projectRect.Height / 2d);

        Assert.True(
            startCenterY < addCenterY && addCenterY < projectCenterY,
            $"Expected Start -> AddProject -> first project order in compact mode, but got StartY={startCenterY}, AddY={addCenterY}, ProjectY={projectCenterY}.{Environment.NewLine}{appData.ReadBootLogTail()}");

        var affordanceElements = addProject.FindAllDescendants()
            .Where(IsVisibleAffordanceElement)
            .Select(element => $"{element.ControlType}:{element.Name}")
            .ToArray();

        // Compact-mode SymbolIcon content is rendered visually but is not exposed as a stable
        // descendant Text/Image/Button peer in WinUI's UIA tree. The smoke contract we can
        // reliably enforce is that the item stays visible in order and does not leak expanded
        // text/button affordances into compact mode.
        Assert.DoesNotContain(affordanceElements, item => item.Contains("ControlType.Text", StringComparison.Ordinal));
        Assert.DoesNotContain(affordanceElements, item => item.Contains("ControlType.Button", StringComparison.Ordinal));
    }

    [SkippableFact]
    public void MinimalMode_Resize_CollapsesLeftPane()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData();
        using var session = WindowsGuiAppSession.LaunchFresh();

        ResizeMainWindow(width: 500, height: 900);
        Thread.Sleep(1500);

        var startItem = session.TryFindByAutomationId("MainNav.Start", TimeSpan.FromSeconds(2));
        var addProjectItem = session.TryFindByAutomationId("MainNav.AddProject", TimeSpan.FromSeconds(2));

        var startVisible = startItem is not null && !TryGetIsOffscreen(startItem);
        var addProjectVisible = addProjectItem is not null && !TryGetIsOffscreen(addProjectItem);

        Assert.False(
            startVisible || addProjectVisible,
            $"Expected minimal mode to collapse the left pane at width=500. StartVisible={startVisible}, AddProjectVisible={addProjectVisible}.{Environment.NewLine}{appData.ReadBootLogTail()}");
    }

    [SkippableFact]
    public void CollapsedPane_AddProject_DoesNotLeakExpandedLabel()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData();
        using var session = WindowsGuiAppSession.LaunchFresh();

        ResizeMainWindow(width: 1400, height: 900);
        Thread.Sleep(1500);
        session.InvokeButton("TitleBar.ToggleSidebar");
        Thread.Sleep(1500);

        var addProject = session.FindByAutomationId("MainNav.AddProject");
        var affordanceElements = addProject.FindAllDescendants()
            .Where(IsVisibleAffordanceElement)
            .Select(element => $"{element.ControlType}:{element.Name}")
            .ToArray();

        Assert.DoesNotContain(affordanceElements, item => item.Contains("ControlType.Text", StringComparison.Ordinal));
        Assert.DoesNotContain(affordanceElements, item => item.Contains("ControlType.Button", StringComparison.Ordinal));
    }

    [SkippableFact]
    public void ActiveSession_CollapsedPane_AddProject_DoesNotLeakExpandedLabel()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData();
        using var session = WindowsGuiAppSession.LaunchFresh();

        ResizeMainWindow(width: 1400, height: 900);
        Thread.Sleep(1500);

        var sessionItem = session.FindByAutomationId("MainNav.Session.gui-session-01");
        session.ActivateElement(sessionItem);
        session.FindByAutomationId("ChatView.CurrentSessionNameButton", TimeSpan.FromSeconds(10));

        session.InvokeButton("TitleBar.ToggleSidebar");
        Thread.Sleep(1500);
        var captureRoot = Path.Combine(Path.GetTempPath(), "SalmonEgg.GuiTests");
        Directory.CreateDirectory(captureRoot);
        session.MainWindow.CaptureToFile(Path.Combine(captureRoot, "nav-collapsed-active-session.png"));

        var addProject = session.FindByAutomationId("MainNav.AddProject");
        var affordanceElements = addProject.FindAllDescendants()
            .Where(IsVisibleAffordanceElement)
            .Select(element => $"{element.ControlType}:{element.Name}")
            .ToArray();

        Assert.DoesNotContain(affordanceElements, item => item.Contains("ControlType.Text", StringComparison.Ordinal));
        Assert.DoesNotContain(affordanceElements, item => item.Contains("ControlType.Button", StringComparison.Ordinal));
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

        if (NativeMethods.TryGetWindowSize(process.MainWindowHandle, out var currentWidth, out var currentHeight)
            && Math.Abs(currentWidth - width) <= 2
            && Math.Abs(currentHeight - height) <= 2)
        {
            return;
        }

        throw new InvalidOperationException("Failed to resize the SalmonEgg window.");
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

        internal static bool TryGetWindowSize(IntPtr hWnd, out int width, out int height)
        {
            if (GetWindowRect(hWnd, out var rect))
            {
                width = rect.Right - rect.Left;
                height = rect.Bottom - rect.Top;
                return true;
            }

            width = 0;
            height = 0;
            return false;
        }
    }

    private static bool IsVisibleAffordanceElement(AutomationElement element)
    {
        if (TryGetIsOffscreen(element))
        {
            return false;
        }

        return element.ControlType == ControlType.Text
            || element.ControlType == ControlType.Button
            || element.ControlType == ControlType.Image;
    }

    private static bool TryGetIsOffscreen(AutomationElement element)
    {
        try
        {
            return element.IsOffscreen;
        }
        catch
        {
            return false;
        }
    }
}
