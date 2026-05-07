using SalmonEgg.Presentation.Core.Services.Shortcuts;

namespace SalmonEgg.Presentation.Core.Tests.Shortcuts;

public sealed class AppShortcutGestureTests
{
    [Theory]
    [InlineData("Ctrl+C")]
    [InlineData("Ctrl+V")]
    [InlineData("Ctrl+X")]
    [InlineData("Ctrl+A")]
    [InlineData("Ctrl+Z")]
    [InlineData("Ctrl+Y")]
    [InlineData("Ctrl+Shift+Z")]
    public void TryParse_RejectsNativeTextEditingReservedGestures(string input)
    {
        Assert.False(AppShortcutGesture.TryParse(input, out _));
    }
}
