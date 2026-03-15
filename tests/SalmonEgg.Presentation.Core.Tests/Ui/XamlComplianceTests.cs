using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Ui;

public sealed class XamlComplianceTests
{
    [Fact]
    public void MainPage_DoesNotDisableFocusOnInteraction()
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\MainPage.xaml");

        Assert.DoesNotContain("AllowFocusOnInteraction=\"False\"", xaml);
    }

    [Theory]
    [InlineData("TitleBarBackButton")]
    [InlineData("TitleBarToggleLeftNavButton")]
    [InlineData("DiffPanelButton")]
    [InlineData("TodoPanelButton")]
    public void MainPage_IconButtonsHaveAutomationName(string elementName)
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\MainPage.xaml");
        var tag = FindElementTag(xaml, elementName);

        Assert.Contains("AutomationProperties.Name", tag);
    }

    [Fact]
    public void MainPage_SearchBoxHasAutomationName()
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\MainPage.xaml");
        var tag = FindElementTag(xaml, "TopSearchBox");

        Assert.True(
            tag.Contains("AutomationProperties.Name") || tag.Contains("x:Uid=\"TopSearchBox\""),
            "TopSearchBox must expose an accessible name via AutomationProperties.Name or x:Uid localization.");
    }

    [Fact]
    public void MainPage_SearchLayoutAvoidsFixedWidths()
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\MainPage.xaml");

        Assert.DoesNotContain("TopSearchBox.Width", xaml);
        Assert.DoesNotContain("MinWidth\" Value=\"420\"", xaml);
        Assert.DoesNotContain("MaxWidth\" Value=\"420\"", xaml);
    }

    [Fact]
    public void MainPage_SearchUsesVirtualizedRepeaters()
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\MainPage.xaml");

        Assert.DoesNotContain("ItemsControl ItemsSource=\"{x:Bind SearchVM.ResultGroups", xaml);
        Assert.DoesNotContain("ItemsControl ItemsSource=\"{x:Bind SearchVM.RecentSearches", xaml);
    }

    [Fact]
    public void MainPage_SearchStringsAreLocalized()
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\MainPage.xaml");

        Assert.DoesNotContain("PlaceholderText=\"搜索\"", xaml);
        Assert.DoesNotContain("AutomationProperties.Name=\"搜索\"", xaml);
        Assert.DoesNotContain("Text=\"最近搜索\"", xaml);
        Assert.DoesNotContain("Text=\"无搜索结果\"", xaml);
        Assert.Contains("x:Uid=\"TopSearchBox\"", xaml);
        Assert.Contains("x:Uid=\"SearchPanelRecentTitle\"", xaml);
        Assert.Contains("x:Uid=\"SearchPanelEmptyText\"", xaml);
    }

    [Fact]
    public void ChatInputArea_IconButtonsAccessibleAndTouchSized()
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\Controls\ChatInputArea.xaml");
        var sendTag = FindElementTag(xaml, "SendButton");
        var cancelTag = FindElementTag(xaml, "CancelButton");

        Assert.Contains("AutomationProperties.Name", sendTag);
        Assert.Contains("AutomationProperties.Name", cancelTag);
        Assert.Contains("MinWidth=\"44\"", sendTag);
        Assert.Contains("MinHeight=\"44\"", sendTag);
        Assert.Contains("MinWidth=\"44\"", cancelTag);
        Assert.Contains("MinHeight=\"44\"", cancelTag);
    }

    [Fact]
    public void ChatInputArea_SendButtonUsesCommandBinding()
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\Controls\ChatInputArea.xaml");
        var sendTag = FindElementTag(xaml, "SendButton");

        Assert.DoesNotContain("Click=\"OnSendClick\"", sendTag);
        Assert.Contains("Command=\"{x:Bind SubmitCommand", sendTag);
    }

    [Fact]
    public void ChatInputArea_AvoidsFixedModeWidth()
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\Controls\ChatInputArea.xaml");

        Assert.DoesNotContain("Width=\"140\"", xaml);
    }

    [Theory]
    [InlineData(@"SalmonEgg\SalmonEgg\Presentation\Views\Chat\ChatView.xaml")]
    [InlineData(@"SalmonEgg\SalmonEgg\Presentation\Views\Start\StartView.xaml")]
    public void OverlayScrim_UsesThemeBrush(string relativePath)
    {
        var xaml = LoadXaml(relativePath);

        Assert.DoesNotContain("Background=\"#40000000\"", xaml);
    }

    [Fact]
    public void AppTheme_DoesNotUseHardcodedTintColors()
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\App.xaml");

        Assert.DoesNotContain("TintColor=\"#", xaml);
        Assert.DoesNotContain("FallbackColor=\"#", xaml);
    }

    [Fact]
    public void AgentProfileEditor_DoesNotUseValueChangedHandlers()
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\Presentation\Views\Settings\AgentProfileEditorPage.xaml");

        Assert.DoesNotContain("ValueChanged=\"OnHeartbeatValueChanged\"", xaml);
        Assert.DoesNotContain("ValueChanged=\"OnTimeoutValueChanged\"", xaml);
    }

    [Fact]
    public void ChatInputArea_DoesNotUseHardcodedWhiteForeground()
    {
        var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\Controls\ChatInputArea.xaml");

        Assert.DoesNotContain("Foreground=\"White\"", xaml);
    }

    private static string LoadXaml(string relativePath)
    {
        var root = FindRepoRoot();
        var fullPath = Path.Combine(root, relativePath);
        return File.ReadAllText(fullPath);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SalmonEgg.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root (SalmonEgg.sln) not found.");
    }

    private static string FindElementTag(string xaml, string elementName)
    {
        var pattern = $"<[^>]*x:Name=\\\"{Regex.Escape(elementName)}\\\"[^>]*>";
        var match = Regex.Match(xaml, pattern, RegexOptions.Singleline);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Element '{elementName}' not found in XAML.");
        }

        return match.Value;
    }
}
