using System.Collections.Immutable;

namespace SalmonEgg.Presentation.Core.Services.Chat.Slash;

public enum SlashParseKind
{
    NotSlashText,
    EditingCommandName,
    EditingArgumentToken
}

public enum SlashCommandSourceKind
{
    Local,
    Remote
}

public enum SlashSuggestionLayer
{
    None,
    CommandName,
    Subcommand
}

public sealed record SlashParseResult
{
    public required SlashParseKind Kind { get; init; }

    public required string RawText { get; init; }

    public required string TrimmedStartText { get; init; }

    public required int LeadingWhitespaceCount { get; init; }

    public required ImmutableArray<string> Tokens { get; init; }

    public required string? CommandToken { get; init; }

    public required ImmutableArray<string> ArgumentTokens { get; init; }

    public required int CurrentTokenIndex { get; init; }

    public required string CurrentTokenText { get; init; }

    public required bool HasTrailingSpace { get; init; }
}

public sealed record SlashCommandSpec
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required SlashCommandSourceKind Source { get; init; }

    public ImmutableArray<string> Aliases { get; init; } = ImmutableArray<string>.Empty;

    public bool Hidden { get; init; }

    public int Order { get; init; }

    public ImmutableArray<SlashCommandSpec> Subcommands { get; init; } = ImmutableArray<SlashCommandSpec>.Empty;

    public string? InputHint { get; init; }
}

public sealed record SlashSuggestionState
{
    public required bool ShowSuggestions { get; init; }

    public required SlashSuggestionLayer ActiveLayer { get; init; }

    public required ImmutableArray<SlashCommandSpec> Items { get; init; }

    public required int SelectedIndex { get; init; }

    public required SlashCommandSpec? SelectedItem { get; init; }

    public required string GhostSuffix { get; init; }
}

public sealed record SlashAcceptanceResult
{
    public required bool Accepted { get; init; }

    public required string NextPromptText { get; init; }

    public required SlashSuggestionState NextSuggestionState { get; init; }
}
