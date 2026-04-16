using SalmonEgg.Domain.Models.Content;
using SalmonEgg.Presentation.ViewModels.Chat;

namespace SalmonEgg.Presentation.Core.Tests.Chat;

public sealed class ChatMessageViewModelMarkdownTests
{
    [Fact]
    public void AssistantText_WithClosedFence_UsesMarkdownReady()
    {
        var vm = ChatMessageViewModel.CreateFromTextContent(
            "m-1",
            new TextContentBlock("```csharp\nvar value = 1;\n```"),
            isOutgoing: false);

        Assert.Equal(ChatMarkdownRenderMode.MarkdownReady, vm.MarkdownRenderMode);
        Assert.True(vm.ShouldRenderMarkdown);
        Assert.False(vm.ShouldRenderPlainText);
    }

    [Fact]
    public void AssistantText_WithUnclosedFence_UsesPlainStreaming()
    {
        var vm = ChatMessageViewModel.CreateFromTextContent(
            "m-2",
            new TextContentBlock("```csharp\nvar value = 1;"),
            isOutgoing: false);

        Assert.Equal(ChatMarkdownRenderMode.PlainStreaming, vm.MarkdownRenderMode);
        Assert.False(vm.ShouldRenderMarkdown);
        Assert.True(vm.ShouldRenderPlainText);
    }

    [Fact]
    public void OutgoingText_AlwaysUsesPlainStreaming()
    {
        var vm = ChatMessageViewModel.CreateFromTextContent(
            "m-3",
            new TextContentBlock("**bold**"),
            isOutgoing: true);

        Assert.Equal(ChatMarkdownRenderMode.PlainStreaming, vm.MarkdownRenderMode);
        Assert.False(vm.ShouldRenderMarkdown);
        Assert.True(vm.ShouldRenderPlainText);
    }

    [Fact]
    public void MarkMarkdownRenderFailed_MakesFallbackSticky()
    {
        var vm = ChatMessageViewModel.CreateFromTextContent(
            "m-4",
            new TextContentBlock("normal text"),
            isOutgoing: false);

        vm.MarkMarkdownRenderFailed();
        vm.TextContent = "```csharp\nConsole.WriteLine(\"ok\");\n```";

        Assert.Equal(ChatMarkdownRenderMode.FallbackPlain, vm.MarkdownRenderMode);
        Assert.True(vm.IsMarkdownFallbackSticky);
        Assert.False(vm.ShouldRenderMarkdown);
        Assert.True(vm.ShouldRenderPlainText);
    }
}
