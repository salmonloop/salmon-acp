using System;
using System.IO;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Ui;

public sealed class ToolCallPillComplianceTests
{
    [Fact]
    public void ToolCallPill_StatusFlagsRefreshBindableVisualState()
    {
        var code = File.ReadAllText(GetRepoPath(@"SalmonEgg\SalmonEgg\Controls\ToolCallPill.xaml.cs"));

        Assert.Contains(
            "DependencyProperty.Register(nameof(IsInProgress), typeof(bool), typeof(ToolCallPill), new PropertyMetadata(false, OnVisualStateInputChanged));",
            code,
            StringComparison.Ordinal);
        Assert.Contains(
            "DependencyProperty.Register(nameof(IsCompleted), typeof(bool), typeof(ToolCallPill), new PropertyMetadata(false, OnVisualStateInputChanged));",
            code,
            StringComparison.Ordinal);
        Assert.Contains(
            "DependencyProperty.Register(nameof(IsFailed), typeof(bool), typeof(ToolCallPill), new PropertyMetadata(false, OnVisualStateInputChanged));",
            code,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ChatStyles_ToolCallPillVisibilityDoesNotDependOnPayloadOnly()
    {
        var xaml = File.ReadAllText(GetRepoPath(@"SalmonEgg\SalmonEgg\Styles\ChatStyles.xaml"));

        Assert.Contains(
            "Visibility=\"{x:Bind ShouldShowToolCallPill, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}\"",
            xaml,
            StringComparison.Ordinal);
    }

    private static string GetRepoPath(string relativePath)
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
