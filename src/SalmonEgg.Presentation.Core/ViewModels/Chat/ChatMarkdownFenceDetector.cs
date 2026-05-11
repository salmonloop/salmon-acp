namespace SalmonEgg.Presentation.ViewModels.Chat;

/// <summary>
/// Provides shared fenced-code parsing rules for chat markdown rendering and interaction affordances.
/// </summary>
public static class ChatMarkdownFenceDetector
{
    /// <summary>
    /// Determines whether the markdown contains at least one closed fenced code block.
    /// </summary>
    /// <param name="text">The markdown text to inspect.</param>
    /// <returns><see langword="true" /> when a closed fenced code block is present; otherwise <see langword="false" />.</returns>
    public static bool HasClosedFence(string? text) => TryExtractFirstFencedCodeBlock(text) is not null;

    /// <summary>
    /// Determines whether the markdown currently contains an unclosed fenced code block.
    /// </summary>
    /// <param name="text">The markdown text to inspect.</param>
    /// <returns><see langword="true" /> when a fence is opened but not closed; otherwise <see langword="false" />.</returns>
    public static bool HasUnclosedFence(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var lines = NormalizeLineEndings(text).Split('\n');
        var isFenceOpen = false;
        var fenceChar = '\0';
        var fenceLength = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimStart();
            if (line.Length < 3)
            {
                continue;
            }

            if (!TryGetFenceHeader(line, out var markerChar, out var markerLength))
            {
                continue;
            }

            if (!isFenceOpen)
            {
                isFenceOpen = true;
                fenceChar = markerChar;
                fenceLength = markerLength;
                continue;
            }

            if (markerChar == fenceChar && markerLength >= fenceLength)
            {
                isFenceOpen = false;
                fenceChar = '\0';
                fenceLength = 0;
            }
        }

        return isFenceOpen;
    }

    /// <summary>
    /// Extracts the first closed fenced code block content from the markdown text.
    /// </summary>
    /// <param name="markdown">The markdown text to inspect.</param>
    /// <returns>The fenced code content without fence markers, or <see langword="null" /> when no closed fence exists.</returns>
    public static string? TryExtractFirstFencedCodeBlock(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return null;
        }

        var normalized = NormalizeLineEndings(markdown);
        var index = 0;

        while (index < normalized.Length)
        {
            var lineStart = index;
            var lineEnd = normalized.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = normalized.Length;
            }

            var line = normalized.Substring(lineStart, lineEnd - lineStart);
            if (TryGetFenceHeader(line.TrimStart(), out var openingFenceChar, out var openingFenceLength))
            {
                var contentStart = lineEnd < normalized.Length ? lineEnd + 1 : lineEnd;
                var searchIndex = contentStart;

                while (searchIndex < normalized.Length)
                {
                    var closingLineStart = searchIndex;
                    var closingLineEnd = normalized.IndexOf('\n', closingLineStart);
                    if (closingLineEnd < 0)
                    {
                        closingLineEnd = normalized.Length;
                    }

                    var closingLine = normalized.Substring(closingLineStart, closingLineEnd - closingLineStart);
                    if (TryGetFenceHeader(closingLine.TrimStart(), out var closingFenceChar, out var closingFenceLength)
                        && closingFenceChar == openingFenceChar
                        && closingFenceLength >= openingFenceLength)
                    {
                        var content = normalized.Substring(contentStart, closingLineStart - contentStart).TrimEnd('\n');
                        return string.IsNullOrEmpty(content) ? null : content;
                    }

                    searchIndex = closingLineEnd < normalized.Length ? closingLineEnd + 1 : closingLineEnd;
                }

                return null;
            }

            index = lineEnd < normalized.Length ? lineEnd + 1 : normalized.Length;
        }

        return null;
    }

    private static string NormalizeLineEndings(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static bool TryGetFenceHeader(string line, out char markerChar, out int markerLength)
    {
        markerChar = '\0';
        markerLength = 0;

        if (string.IsNullOrEmpty(line))
        {
            return false;
        }

        var first = line[0];
        if (first != '`' && first != '~')
        {
            return false;
        }

        var index = 0;
        while (index < line.Length && line[index] == first)
        {
            index++;
        }

        if (index < 3)
        {
            return false;
        }

        markerChar = first;
        markerLength = index;
        return true;
    }
}
