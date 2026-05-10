namespace SalmonEgg.Presentation.Core.Services.Chat.Slash;

public sealed class SlashInteractionCoordinator
{
    private readonly SlashCommandRegistry _registry;
    private SlashParseResult _parseResult = SlashInputParser.Parse(string.Empty);
    private SlashSuggestionState _suggestionState = CreateEmptySuggestionState();

    public SlashInteractionCoordinator(SlashCommandRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public SlashSuggestionState CurrentSuggestionState => _suggestionState;

    public SlashSuggestionState UpdatePrompt(string? text)
    {
        _parseResult = SlashInputParser.Parse(text);
        _suggestionState = SlashSuggestionEngine.Evaluate(_parseResult, _registry);
        return _suggestionState;
    }

    public SlashSuggestionState MoveSelection(int delta)
    {
        if (!_suggestionState.ShowSuggestions || _suggestionState.Items.IsDefaultOrEmpty)
        {
            return _suggestionState;
        }

        _suggestionState = SlashSuggestionEngine.Reselect(
            _suggestionState,
            _parseResult,
            _suggestionState.SelectedIndex + delta);

        return _suggestionState;
    }

    public SlashAcceptanceResult AcceptSelection()
    {
        if (!_suggestionState.ShowSuggestions || _suggestionState.SelectedItem is null)
        {
            return new SlashAcceptanceResult
            {
                Accepted = false,
                NextPromptText = _parseResult.RawText,
                NextSuggestionState = _suggestionState
            };
        }

        var nextPromptText = string.Concat(
            new string(' ', _parseResult.LeadingWhitespaceCount),
            "/",
            _suggestionState.SelectedItem.Name,
            " ");

        _parseResult = SlashInputParser.Parse(nextPromptText);
        _suggestionState = SlashSuggestionEngine.Evaluate(_parseResult, _registry);

        return new SlashAcceptanceResult
        {
            Accepted = true,
            NextPromptText = nextPromptText,
            NextSuggestionState = _suggestionState
        };
    }

    private static SlashSuggestionState CreateEmptySuggestionState()
    {
        return new SlashSuggestionState
        {
            ShowSuggestions = false,
            ActiveLayer = SlashSuggestionLayer.None,
            Items = [],
            SelectedIndex = -1,
            SelectedItem = null,
            GhostSuffix = string.Empty
        };
    }
}
