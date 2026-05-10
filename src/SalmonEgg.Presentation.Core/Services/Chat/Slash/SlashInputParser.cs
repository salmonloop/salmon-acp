using System.Collections.Immutable;

namespace SalmonEgg.Presentation.Core.Services.Chat.Slash;

public static class SlashInputParser
{
    public static SlashParseResult Parse(string? rawText)
    {
        var raw = rawText ?? string.Empty;
        var leadingWhitespaceCount = CountLeadingWhitespace(raw);
        var trimmedStart = raw[leadingWhitespaceCount..];

        if (!trimmedStart.StartsWith("/", StringComparison.Ordinal))
        {
            return CreateNotSlashText(raw, trimmedStart, leadingWhitespaceCount);
        }

        var hasTrailingSpace = raw.Length > 0 && char.IsWhiteSpace(raw[^1]);
        var content = trimmedStart[1..];
        var tokens = Tokenize(content);
        var commandToken = tokens.Length > 0 ? tokens[0] : string.Empty;
        var argumentTokens = tokens.Length > 1
            ? ImmutableArray.Create(tokens[1..])
            : ImmutableArray<string>.Empty;
        var currentTokenIndex = hasTrailingSpace
            ? tokens.Length
            : Math.Max(tokens.Length - 1, 0);
        var currentTokenText = hasTrailingSpace || tokens.Length == 0
            ? string.Empty
            : tokens[^1];

        return new SlashParseResult
        {
            Kind = hasTrailingSpace || argumentTokens.Length > 0
                ? SlashParseKind.EditingArgumentToken
                : SlashParseKind.EditingCommandName,
            RawText = raw,
            TrimmedStartText = trimmedStart,
            LeadingWhitespaceCount = leadingWhitespaceCount,
            Tokens = ImmutableArray.Create(tokens),
            CommandToken = commandToken,
            ArgumentTokens = argumentTokens,
            CurrentTokenIndex = currentTokenIndex,
            CurrentTokenText = currentTokenText,
            HasTrailingSpace = hasTrailingSpace
        };
    }

    private static SlashParseResult CreateNotSlashText(string rawText, string trimmedStartText, int leadingWhitespaceCount)
    {
        return new SlashParseResult
        {
            Kind = SlashParseKind.NotSlashText,
            RawText = rawText,
            TrimmedStartText = trimmedStartText,
            LeadingWhitespaceCount = leadingWhitespaceCount,
            Tokens = ImmutableArray<string>.Empty,
            CommandToken = null,
            ArgumentTokens = ImmutableArray<string>.Empty,
            CurrentTokenIndex = 0,
            CurrentTokenText = string.Empty,
            HasTrailingSpace = false
        };
    }

    private static int CountLeadingWhitespace(string rawText)
    {
        var index = 0;
        while (index < rawText.Length && char.IsWhiteSpace(rawText[index]))
        {
            index++;
        }

        return index;
    }

    private static string[] Tokenize(string content)
    {
        return content.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }
}
