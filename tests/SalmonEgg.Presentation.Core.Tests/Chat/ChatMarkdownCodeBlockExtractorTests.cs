using SalmonEgg.Presentation.ViewModels.Chat;

namespace SalmonEgg.Presentation.Core.Tests.Chat;

public sealed class ChatMarkdownCodeBlockExtractorTests
{
    [Fact]
    public void TryExtractFirstFencedCodeBlock_ReturnsContentWithoutFenceOrLanguage()
    {
        const string markdown = """
            Before.

            ```csharp
            var done = true;
            Console.WriteLine(done);
            ```

            After.
            """;

        var extracted = ChatMarkdownCodeBlockExtractor.TryExtractFirstFencedCodeBlock(markdown);

        Assert.Equal("var done = true;\nConsole.WriteLine(done);", extracted);
    }

    [Fact]
    public void TryExtractFirstFencedCodeBlock_ReturnsNullForUnclosedFence()
    {
        const string markdown = """
            ```csharp
            var partial = true;
            """;

        Assert.Null(ChatMarkdownCodeBlockExtractor.TryExtractFirstFencedCodeBlock(markdown));
    }

    [Fact]
    public void TryExtractFirstFencedCodeBlock_PreservesInnerBlankLines()
    {
        const string markdown = """
            ```
            first

            second
            ```
            """;

        var extracted = ChatMarkdownCodeBlockExtractor.TryExtractFirstFencedCodeBlock(markdown);

        Assert.Equal("first\n\nsecond", extracted);
    }

    [Fact]
    public void TryExtractFirstFencedCodeBlock_SupportsTildeFences()
    {
        const string markdown = """
            ~~~json
            {
              "done": true
            }
            ~~~
            """;

        var extracted = ChatMarkdownCodeBlockExtractor.TryExtractFirstFencedCodeBlock(markdown);

        Assert.Equal("{\n  \"done\": true\n}", extracted);
    }
}
