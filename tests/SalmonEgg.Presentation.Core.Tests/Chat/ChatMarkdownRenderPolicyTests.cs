using SalmonEgg.Presentation.ViewModels.Chat;

namespace SalmonEgg.Presentation.Core.Tests.Chat;

public sealed class ChatMarkdownRenderPolicyTests
{
    [Theory]
    [InlineData("text", false, "hello", ChatMarkdownRenderMode.PlainStreaming)]
    [InlineData("text", true, "hello", ChatMarkdownRenderMode.PlainStreaming)]
    [InlineData("tool_call", false, "hello", ChatMarkdownRenderMode.PlainStreaming)]
    public void Resolve_AppliesScopeRules(
        string contentType,
        bool isOutgoing,
        string text,
        ChatMarkdownRenderMode expectedMode)
    {
        var mode = ChatMarkdownRenderPolicy.Resolve(
            contentType,
            isOutgoing,
            text,
            isFallbackSticky: false);

        Assert.Equal(expectedMode, mode);
    }

    [Fact]
    public void Resolve_WhenFenceIsUnclosed_StaysPlainStreaming()
    {
        const string text = "```csharp\nvar a = 1;";

        var mode = ChatMarkdownRenderPolicy.Resolve("text", false, text, isFallbackSticky: false);

        Assert.Equal(ChatMarkdownRenderMode.PlainStreaming, mode);
    }

    [Fact]
    public void Resolve_WhenFenceIsClosed_SwitchesToMarkdownReady()
    {
        const string text = "```csharp\nvar a = 1;\n```";

        var mode = ChatMarkdownRenderPolicy.Resolve("text", false, text, isFallbackSticky: false);

        Assert.Equal(ChatMarkdownRenderMode.MarkdownReady, mode);
    }

    [Fact]
    public void Resolve_WhenTildeFenceIsUnclosed_StaysPlainStreaming()
    {
        const string text = "~~~csharp\nvar a = 1;";

        var mode = ChatMarkdownRenderPolicy.Resolve("text", false, text, isFallbackSticky: false);

        Assert.Equal(ChatMarkdownRenderMode.PlainStreaming, mode);
    }

    [Fact]
    public void Resolve_WhenTildeFenceIsClosed_SwitchesToMarkdownReady()
    {
        const string text = "~~~csharp\nvar a = 1;\n~~~";

        var mode = ChatMarkdownRenderPolicy.Resolve("text", false, text, isFallbackSticky: false);

        Assert.Equal(ChatMarkdownRenderMode.MarkdownReady, mode);
    }

    [Theory]
    [InlineData("# heading")]
    [InlineData("- item")]
    [InlineData("> quote")]
    [InlineData("Use `code` here")]
    [InlineData("**bold** text")]
    [InlineData("[label](https://example.com)")]
    public void Resolve_WhenMarkdownSyntaxExists_SwitchesToMarkdownReady(string text)
    {
        var mode = ChatMarkdownRenderPolicy.Resolve("text", false, text, isFallbackSticky: false);

        Assert.Equal(ChatMarkdownRenderMode.MarkdownReady, mode);
    }

    [Fact]
    public void Resolve_WhenFallbackSticky_ReturnsFallbackPlain()
    {
        var mode = ChatMarkdownRenderPolicy.Resolve("text", false, "hello", isFallbackSticky: true);

        Assert.Equal(ChatMarkdownRenderMode.FallbackPlain, mode);
    }
}
