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
}
