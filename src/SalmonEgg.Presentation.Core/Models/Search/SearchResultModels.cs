using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SalmonEgg.Presentation.Models.Search;

public enum SearchResultKind
{
    Session,
    Project,
    Command,
    Setting,
    File,
    Placeholder
}

public sealed class SearchResultItem
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public string? Subtitle { get; init; }

    public SearchResultKind Kind { get; init; }

    public string? IconGlyph { get; init; }

    public string? Tag { get; init; }
}

public sealed class SearchHistoryItem
{
    public required string Query { get; init; }
}

public sealed class SearchResultGroup
{
    public required string Title { get; init; }

    public required string Name { get; init; }

    public int Priority { get; init; }

    public List<SearchResultItem> Items { get; } = new();
}

public enum SearchSuggestionEntryKind
{
    Result,
    History,
    Status
}

public sealed class SearchSuggestionEntry
{
    public required string AutomationId { get; init; }

    public required string Title { get; init; }

    public string? Subtitle { get; init; }

    public string? SectionTitle { get; init; }

    public string? IconGlyph { get; init; }

    public SearchSuggestionEntryKind Kind { get; init; }

    public SearchResultItem? ResultItem { get; init; }

    public string? HistoryQuery { get; init; }

    public bool IsActionable => Kind != SearchSuggestionEntryKind.Status;

    public string SecondaryText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Subtitle) && !string.IsNullOrWhiteSpace(SectionTitle))
            {
                return $"{SectionTitle} · {Subtitle}";
            }

            if (!string.IsNullOrWhiteSpace(Subtitle))
            {
                return Subtitle!;
            }

            if (!string.IsNullOrWhiteSpace(SectionTitle))
            {
                return SectionTitle!;
            }

            return string.Empty;
        }
    }
}

public enum GlobalSearchViewState
{
    Idle = 0,
    Loading = 1,
    Results = 2,
    Empty = 3,
    Error = 4
}

public sealed partial record GlobalSearchSourceSnapshot(
    ImmutableArray<GlobalSearchSessionSource> Sessions,
    ImmutableArray<GlobalSearchProjectSource> Projects);

public sealed partial record GlobalSearchSessionSource(
    string ConversationId,
    string Title,
    string? Cwd);

public sealed partial record GlobalSearchProjectSource(
    string ProjectId,
    string Name,
    string RootPath);

public sealed partial record GlobalSearchGroupSnapshot(
    string Name,
    string Title,
    int Priority,
    ImmutableArray<GlobalSearchItemSnapshot> Items);

public sealed partial record GlobalSearchItemSnapshot(
    string Id,
    string Title,
    string? Subtitle,
    SearchResultKind Kind,
    string? IconGlyph,
    string? Tag);

public sealed partial record GlobalSearchSnapshot(
    ImmutableArray<GlobalSearchGroupSnapshot> Groups);
