namespace SalmonEgg.Presentation.ViewModels.Chat;

public static class ChatMarkdownRenderPolicy
{
    public static ChatMarkdownRenderMode Resolve(
        string? contentType,
        bool isOutgoing,
        string? text,
        bool isFallbackSticky)
    {
        if (isFallbackSticky)
        {
            return ChatMarkdownRenderMode.FallbackPlain;
        }

        if (isOutgoing || !string.Equals(contentType, "text", StringComparison.Ordinal))
        {
            return ChatMarkdownRenderMode.PlainStreaming;
        }

        return ShouldRenderMarkdown(text) && !ChatMarkdownFenceDetector.HasUnclosedFence(text)
            ? ChatMarkdownRenderMode.MarkdownReady
            : ChatMarkdownRenderMode.PlainStreaming;
    }

    private static bool ShouldRenderMarkdown(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (normalized.Contains("```", StringComparison.Ordinal)
            || normalized.Contains("~~~", StringComparison.Ordinal))
        {
            return true;
        }

        var lines = normalized.Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimStart();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("#", StringComparison.Ordinal)
                || line.StartsWith(">", StringComparison.Ordinal)
                || line.StartsWith("- ", StringComparison.Ordinal)
                || line.StartsWith("* ", StringComparison.Ordinal)
                || line.StartsWith("|", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return HasInlineMarkdown(normalized);
    }

    private static bool HasInlineMarkdown(string text)
    {
        return text.Contains("`", StringComparison.Ordinal)
            || ContainsDelimited(text, "**")
            || ContainsDelimited(text, "__")
            || ContainsDelimited(text, "*")
            || ContainsDelimited(text, "_")
            || ContainsDelimited(text, "~~")
            || ContainsLinkLikeSyntax(text);
    }

    private static bool ContainsDelimited(string text, string marker)
    {
        var first = text.IndexOf(marker, StringComparison.Ordinal);
        if (first < 0)
        {
            return false;
        }

        var second = text.IndexOf(marker, first + marker.Length, StringComparison.Ordinal);
        return second > first;
    }

    private static bool ContainsLinkLikeSyntax(string text)
    {
        var labelStart = text.IndexOf('[', StringComparison.Ordinal);
        if (labelStart < 0)
        {
            return false;
        }

        var labelEnd = text.IndexOf(']', labelStart + 1);
        if (labelEnd < 0 || labelEnd + 1 >= text.Length || text[labelEnd + 1] != '(')
        {
            return false;
        }

        var destinationEnd = text.IndexOf(')', labelEnd + 2);
        return destinationEnd > labelEnd + 2;
    }

}
