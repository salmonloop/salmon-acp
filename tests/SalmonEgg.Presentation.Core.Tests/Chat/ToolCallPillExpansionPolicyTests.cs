using SalmonEgg.Presentation.ViewModels.Chat;

namespace SalmonEgg.Presentation.Core.Tests.Chat;

public sealed class ToolCallPillExpansionPolicyTests
{
    [Fact]
    public void ResolveDefaultExpanded_WhenToolCallIsIncomplete_DefaultsExpanded()
    {
        var isExpanded = ToolCallPillExpansionPolicy.ResolveDefaultExpanded(isCompleted: false);

        Assert.True(isExpanded);
    }

    [Fact]
    public void ResolveDefaultExpanded_WhenToolCallIsCompleted_DefaultsCollapsed()
    {
        var isExpanded = ToolCallPillExpansionPolicy.ResolveDefaultExpanded(isCompleted: true);

        Assert.False(isExpanded);
    }

    [Fact]
    public void ShouldShowInlineContent_WhenIncompleteButManuallyCollapsed_ReturnsFalse()
    {
        var shouldShow = ToolCallPillExpansionPolicy.ShouldShowInlineContent(
            hasInlineContent: true,
            isExpanded: false);

        Assert.False(shouldShow);
    }

    [Fact]
    public void ShouldShowInlineContent_WhenCompletedButManuallyExpanded_ReturnsTrue()
    {
        var shouldShow = ToolCallPillExpansionPolicy.ShouldShowInlineContent(
            hasInlineContent: true,
            isExpanded: true);

        Assert.True(shouldShow);
    }
}
