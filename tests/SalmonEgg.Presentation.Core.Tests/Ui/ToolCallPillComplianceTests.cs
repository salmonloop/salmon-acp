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

    private static string GetRepoPath(string relativePath)
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
