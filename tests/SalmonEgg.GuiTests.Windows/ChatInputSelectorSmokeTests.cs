using System;
using System.Linq;

namespace SalmonEgg.GuiTests.Windows;

public sealed class ChatInputSelectorSmokeTests
{
    [SkippableFact]
    public void StartComposer_LoadsModeSelectorWithoutCrashing()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData();
        using var session = WindowsGuiAppSession.LaunchFresh();

        Assert.True(
            session.WaitUntilOnscreen("StartView.ModeSelector", TimeSpan.FromSeconds(6)),
            $"Expected start mode selector to be visible.{Environment.NewLine}{appData.ReadBootLogTail()}");
    }

    [SkippableFact]
    public void ChatComposer_ForExistingSession_UsesVisibleModeSelectorSubsetOnly()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData(withContent: true);
        using var session = WindowsGuiAppSession.LaunchFresh();

        var sessionItem = session.FindByAutomationId("MainNav.Session.gui-session-01", TimeSpan.FromSeconds(15));
        session.ClickElement(sessionItem);

        Assert.True(
            session.WaitUntilOnscreen("ChatView.CurrentSessionTitle", TimeSpan.FromSeconds(10)),
            $"Session navigation did not activate the chat view. Visible=[{string.Join(" | ", session.GetVisibleTexts())}]");

        if (session.TryFindByAutomationId("ChatView.LoadingOverlay", TimeSpan.FromSeconds(2)) is not null)
        {
            Assert.True(
                session.WaitUntilHidden("ChatView.LoadingOverlay", TimeSpan.FromSeconds(10)),
                "Loading overlay did not disappear before validating the chat composer selector subset.");
        }

        Assert.True(
            session.WaitUntilOnscreen("ChatInputArea.ModeSelector", TimeSpan.FromSeconds(6)),
            $"Expected formal chat composer to keep the mode selector visible. Visible=[{string.Join(" | ", session.GetVisibleTexts())}]");
    }
}
