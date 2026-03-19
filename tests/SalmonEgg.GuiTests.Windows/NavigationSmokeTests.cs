using System;
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
    public void MoreSessionsDialog_SelectsOverflowSession_AndUpdatesChatHeader()
    {
        using var appData = GuiAppDataScope.CreateDeterministicLeftNavData(sessionCount: 21);
        using var session = WindowsGuiAppSession.LaunchFresh();

        session.ActivateElement(session.FindByAutomationId("MainNav.More.project-1"));

        var dialog = session.FindByAutomationId("SessionsDialog", TimeSpan.FromSeconds(10));
        var dialogSession = session.FindFirstDescendantByControlType(dialog, ControlType.ListItem, TimeSpan.FromSeconds(10));

        session.ActivateElement(dialogSession);

        var chatHeader = session.FindByAutomationId("ChatView.CurrentSessionNameButton", TimeSpan.FromSeconds(10));

        Assert.NotNull(dialog);
        Assert.False(string.IsNullOrWhiteSpace(chatHeader.Name));
    }
}
