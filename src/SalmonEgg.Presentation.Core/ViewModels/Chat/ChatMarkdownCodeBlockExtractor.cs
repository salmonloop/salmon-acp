namespace SalmonEgg.Presentation.ViewModels.Chat;

public static class ChatMarkdownCodeBlockExtractor
{
    public static string? TryExtractFirstFencedCodeBlock(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return null;
        }

        var span = markdown.AsSpan();
        var openingFenceStart = span.IndexOf("```", StringComparison.Ordinal);
        if (openingFenceStart < 0)
        {
            return null;
        }

        var contentStart = openingFenceStart + 3;
        while (contentStart < span.Length && span[contentStart] is not '\r' and not '\n')
        {
            contentStart++;
        }

        contentStart = SkipLineBreak(span, contentStart);
        var closingFenceStart = span[contentStart..].IndexOf("```", StringComparison.Ordinal);
        if (closingFenceStart < 0)
        {
            return null;
        }

        var code = TrimTrailingLineBreaks(span.Slice(contentStart, closingFenceStart));
        return code.IsEmpty ? null : code.ToString();
    }

    private static int SkipLineBreak(ReadOnlySpan<char> span, int index)
    {
        if (index < span.Length && span[index] == '\r')
        {
            index++;
        }

        if (index < span.Length && span[index] == '\n')
        {
            index++;
        }

        return index;
    }

    private static ReadOnlySpan<char> TrimTrailingLineBreaks(ReadOnlySpan<char> span)
    {
        var end = span.Length;
        while (end > 0 && span[end - 1] is '\r' or '\n')
        {
            end--;
        }

        return span[..end];
    }
}
