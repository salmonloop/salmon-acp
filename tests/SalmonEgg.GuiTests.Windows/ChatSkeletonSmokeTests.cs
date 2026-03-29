using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using Xunit;

namespace SalmonEgg.GuiTests.Windows;

public sealed class ChatSkeletonSmokeTests
{
    [SkippableFact]
    public void SelectSessionWithContent_ShowsSkeletonLoader_ThenContent()
    {
        // Use withContent: true to ensure there are messages to be rendered,
        // which triggers the "render hold" logic in ChatView.xaml.cs.
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData(sessionCount: 1, withContent: true);
        using var session = WindowsGuiAppSession.LaunchFresh();

        var sessionItem = session.FindByAutomationId("MainNav.Session.gui-session-01");
        session.ActivateElement(sessionItem);

        var loadingOverlay = WaitForLoadingOverlay(session, "select-session-with-content");

        // Wait for it to disappear (content rendered)
        var isHidden = session.WaitUntilHidden("ChatView.LoadingOverlay", TimeSpan.FromSeconds(10));
        Assert.True(isHidden, "Loading overlay (skeleton) did not disappear after content should have loaded.");

        // Verify content is now visible
        var chatHeader = session.FindByAutomationId("ChatView.CurrentSessionNameButton");
        Assert.NotNull(chatHeader);
        Assert.Contains("GUI Session 01", chatHeader.Name, StringComparison.Ordinal);
    }

    [SkippableFact]
    public void SelectSessionWithLongTranscript_AutoScrollsToLatestMessageAfterLoad()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData(
            sessionCount: 1,
            withContent: true,
            messageCountPerSession: 60);
        using var session = WindowsGuiAppSession.LaunchFresh();

        var sessionItem = session.FindByAutomationId("MainNav.Session.gui-session-01");
        session.ActivateElement(sessionItem);

        if (session.TryFindByAutomationId("ChatView.LoadingOverlay", TimeSpan.FromSeconds(2)) is not null)
        {
            var isHidden = session.WaitUntilHidden("ChatView.LoadingOverlay", TimeSpan.FromSeconds(10));
            Assert.True(isHidden, "Loading overlay did not disappear after the long transcript should have loaded.");
        }

        var messagesList = session.FindByAutomationId("ChatView.MessagesList", TimeSpan.FromSeconds(10));
        var lastMessageText = "GUI Session 01 message 060";

        var lastMessageVisible = session.FindVisibleText(
            lastMessageText,
            messagesList,
            TimeSpan.FromSeconds(4));

        Assert.NotNull(lastMessageVisible);
    }

    [SkippableFact]
    public void SelectRemoteSessionWithSlowReplay_AutoScrollsToLatestMessageAfterHydration()
    {
        var previousSlowLoadDelay = Environment.GetEnvironmentVariable("SALMONEGG_GUI_SLOW_SESSION_LOAD_MS");
        Environment.SetEnvironmentVariable("SALMONEGG_GUI_SLOW_SESSION_LOAD_MS", "1500");

        try
        {
            using var appData = GuiAppDataScope.CreateDeterministicSlowRemoteReplayData(
                cachedMessageCount: 1,
                replayMessageCount: 60);
            using var session = WindowsGuiAppSession.LaunchFresh();

            var sessionItem = session.FindByAutomationId("MainNav.Session.gui-remote-conversation-01", TimeSpan.FromSeconds(15));
            session.ActivateElement(sessionItem);

            var sawOverlayStatus = session.WaitUntilVisible("ChatView.LoadingOverlayStatus", TimeSpan.FromSeconds(10));
            Assert.True(sawOverlayStatus, "Slow remote replay did not expose ChatView.LoadingOverlayStatus.");

            var overlayHidden = session.WaitUntilHidden("ChatView.LoadingOverlay", TimeSpan.FromSeconds(30));
            Assert.True(overlayHidden, "Slow remote replay overlay did not disappear after the transcript should have hydrated.");

            var messagesList = session.FindByAutomationId("ChatView.MessagesList", TimeSpan.FromSeconds(10));
            var lastMessageVisible = session.TryFindVisibleText(
                "GUI Remote Session 01 replay 060",
                messagesList,
                TimeSpan.FromSeconds(8));

            if (lastMessageVisible is null)
            {
                var captureRoot = Path.Combine(Path.GetTempPath(), "SalmonEgg.GuiTests");
                Directory.CreateDirectory(captureRoot);
                var screenshotPath = Path.Combine(
                    captureRoot,
                    $"slow-remote-replay-scroll-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");
                session.MainWindow.CaptureToFile(screenshotPath);

                var visibleTexts = session.GetVisibleTexts(messagesList);
                var bootLogTail = appData.ReadBootLogTail();
                throw new Xunit.Sdk.XunitException(
                    $"Latest replay message was not visible after slow remote hydration. Screenshot: {screenshotPath}{Environment.NewLine}Visible texts: [{string.Join(", ", visibleTexts)}]{Environment.NewLine}boot.log:{Environment.NewLine}{bootLogTail}");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("SALMONEGG_GUI_SLOW_SESSION_LOAD_MS", previousSlowLoadDelay);
        }
    }

    [SkippableFact]
    public void SelectRemoteSession_RepeatedClicksWithLocalDetour_DoesNotHangAndHydratesLatestSelection()
    {
        var previousSlowLoadDelay = Environment.GetEnvironmentVariable("SALMONEGG_GUI_SLOW_SESSION_LOAD_MS");
        Environment.SetEnvironmentVariable("SALMONEGG_GUI_SLOW_SESSION_LOAD_MS", "2000");

        try
        {
            using var appData = GuiAppDataScope.CreateDeterministicSlowRemoteReplayData(
                cachedMessageCount: 1,
                replayMessageCount: 24,
                includeLocalConversation: true,
                localMessageCount: 4);
            using var session = WindowsGuiAppSession.LaunchFresh();

            var remoteItem = session.FindByAutomationId("MainNav.Session.gui-remote-conversation-01", TimeSpan.FromSeconds(15));
            var localItem = session.FindByAutomationId("MainNav.Session.gui-local-conversation-01", TimeSpan.FromSeconds(15));

            session.ActivateElement(remoteItem);

            var sawInitialRemoteStatus = session.WaitUntilVisible("ChatView.LoadingOverlayStatus", TimeSpan.FromSeconds(10));
            Assert.True(sawInitialRemoteStatus, "Initial remote selection did not expose ChatView.LoadingOverlayStatus.");

            session.ActivateElement(remoteItem);
            session.ActivateElement(localItem);

            var localHeader = WaitForSessionHeader(
                session,
                expectedTitle: "GUI Local Session 01",
                scenario: "repeated-remote-clicks-local-detour-local",
                appData);
            Assert.Contains("GUI Local Session 01", localHeader.Name, StringComparison.Ordinal);

            session.ActivateElement(remoteItem);
            session.ActivateElement(remoteItem);

            var sawFinalRemoteStatus = session.WaitUntilVisible("ChatView.LoadingOverlayStatus", TimeSpan.FromSeconds(10));
            Assert.True(sawFinalRemoteStatus, "Final remote reselection did not expose ChatView.LoadingOverlayStatus.");

            var overlayHidden = session.WaitUntilHidden("ChatView.LoadingOverlay", TimeSpan.FromSeconds(40));
            if (!overlayHidden)
            {
                ThrowWithScreenshot(
                    session,
                    appData,
                    "repeated-remote-clicks-local-detour-overlay-stuck",
                    $"Repeated remote reselection stayed stuck behind the loading overlay. Visible texts: [{string.Join(", ", session.GetVisibleTexts())}]");
            }

            var remoteHeader = WaitForSessionHeader(
                session,
                expectedTitle: "GUI Remote Session 01",
                scenario: "repeated-remote-clicks-local-detour-remote",
                appData);
            Assert.Contains("GUI Remote Session 01", remoteHeader.Name, StringComparison.Ordinal);

            var messagesList = session.FindByAutomationId("ChatView.MessagesList", TimeSpan.FromSeconds(10));
            var lastMessageVisible = session.TryFindVisibleText(
                "GUI Remote Session 01 replay 024",
                messagesList,
                TimeSpan.FromSeconds(8));

            if (lastMessageVisible is null)
            {
                ThrowWithScreenshot(
                    session,
                    appData,
                    "repeated-remote-clicks-local-detour-scroll",
                    $"Latest remote replay message was not visible after repeated remote clicks with a local detour. Visible texts: [{string.Join(", ", session.GetVisibleTexts(messagesList))}]");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("SALMONEGG_GUI_SLOW_SESSION_LOAD_MS", previousSlowLoadDelay);
        }
    }

    private static AutomationElement WaitForLoadingOverlay(WindowsGuiAppSession session, string scenario)
    {
        var timeline = new List<string>();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            var loadingOverlay = session.TryFindByAutomationId("ChatView.LoadingOverlay", TimeSpan.FromMilliseconds(100));
            if (loadingOverlay is not null)
            {
                return loadingOverlay;
            }

            var headerVisible = session.TryFindByAutomationId("ChatView.CurrentSessionNameButton", TimeSpan.FromMilliseconds(100)) is not null;
            var messagesVisible = session.TryFindByAutomationId("ChatView.MessagesList", TimeSpan.FromMilliseconds(100)) is not null;
            var interestingIds = session.MainWindow
                .FindAllDescendants()
                .Select(TryGetAutomationId)
                .Where(automationId =>
                    !string.IsNullOrWhiteSpace(automationId) &&
                    (automationId.StartsWith("ChatView.", StringComparison.Ordinal)
                     || automationId.StartsWith("MainNav.", StringComparison.Ordinal)))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(automationId => automationId, StringComparer.Ordinal);

            timeline.Add(
                $"{DateTime.UtcNow:HH:mm:ss.fff} header={headerVisible} messages={messagesVisible} ids=[{string.Join(", ", interestingIds)}]");

            Thread.Sleep(150);
        }

        var captureRoot = Path.Combine(Path.GetTempPath(), "SalmonEgg.GuiTests");
        Directory.CreateDirectory(captureRoot);
        var screenshotPath = Path.Combine(
            captureRoot,
            $"chat-skeleton-{scenario}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");
        session.MainWindow.CaptureToFile(screenshotPath);

        throw new Xunit.Sdk.XunitException(
            $"Loading overlay was not found for scenario '{scenario}'. Screenshot: {screenshotPath}{Environment.NewLine}{string.Join(Environment.NewLine, timeline)}");
    }

    private static string? TryGetAutomationId(AutomationElement element)
    {
        try
        {
            return element.AutomationId;
        }
        catch
        {
            return null;
        }
    }

    private static AutomationElement WaitForSessionHeader(
        WindowsGuiAppSession session,
        string expectedTitle,
        string scenario,
        GuiAppDataScope appData)
    {
        var timeline = new List<string>();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(12);

        while (DateTime.UtcNow < deadline)
        {
            var header = session.TryFindByAutomationId("ChatView.CurrentSessionNameButton", TimeSpan.FromMilliseconds(100));
            var headerName = header?.Name ?? "<missing>";
            var overlayVisible = session.TryFindByAutomationId("ChatView.LoadingOverlay", TimeSpan.FromMilliseconds(100)) is not null;
            var statusVisible = session.TryFindByAutomationId("ChatView.LoadingOverlayStatus", TimeSpan.FromMilliseconds(100)) is not null;

            timeline.Add(
                $"{DateTime.UtcNow:HH:mm:ss.fff} header={headerName} overlay={overlayVisible} status={statusVisible}");

            if (header is not null
                && headerName.Contains(expectedTitle, StringComparison.Ordinal))
            {
                return header;
            }

            Thread.Sleep(150);
        }

        ThrowWithScreenshot(
            session,
            appData,
            scenario,
            $"Expected header containing '{expectedTitle}' was not observed.{Environment.NewLine}{string.Join(Environment.NewLine, timeline)}");
        throw new Xunit.Sdk.XunitException("Unreachable");
    }

    private static void ThrowWithScreenshot(
        WindowsGuiAppSession session,
        GuiAppDataScope appData,
        string scenario,
        string message)
    {
        var captureRoot = Path.Combine(Path.GetTempPath(), "SalmonEgg.GuiTests");
        Directory.CreateDirectory(captureRoot);
        var screenshotPath = Path.Combine(
            captureRoot,
            $"{scenario}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");
        session.MainWindow.CaptureToFile(screenshotPath);

        throw new Xunit.Sdk.XunitException(
            $"{message}{Environment.NewLine}Screenshot: {screenshotPath}{Environment.NewLine}boot.log:{Environment.NewLine}{appData.ReadBootLogTail()}");
    }
}
