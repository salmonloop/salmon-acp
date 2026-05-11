namespace SalmonEgg.Presentation.ViewModels.Chat;

public static class ChatMarkdownCodeBlockExtractor
{
    public static string? TryExtractFirstFencedCodeBlock(string? markdown)
        => ChatMarkdownFenceDetector.TryExtractFirstFencedCodeBlock(markdown);
}
