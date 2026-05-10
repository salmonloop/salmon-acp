using System.Collections.Immutable;

namespace SalmonEgg.Presentation.Core.Services.Chat.Slash;

public static class SlashSuggestionEngine
{
    public static SlashSuggestionState Evaluate(
        SlashParseResult parseResult,
        SlashCommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(registry);

        if (parseResult.Kind != SlashParseKind.EditingCommandName)
        {
            return Empty();
        }

        var commandPrefix = parseResult.CommandToken ?? string.Empty;
        var items = registry.FindCommandSuggestions(commandPrefix).ToImmutableArray();
        if (items.IsDefaultOrEmpty)
        {
            return Empty();
        }

        return CreateState(items, 0, commandPrefix);
    }

    public static SlashSuggestionState Reselect(
        SlashSuggestionState state,
        SlashParseResult parseResult,
        int selectedIndex)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        if (!state.ShowSuggestions || state.Items.IsDefaultOrEmpty)
        {
            return state;
        }

        var clampedIndex = Math.Clamp(selectedIndex, 0, state.Items.Length - 1);
        return CreateState(state.Items, clampedIndex, parseResult.CommandToken ?? string.Empty);
    }

    private static SlashSuggestionState CreateState(
        ImmutableArray<SlashCommandSpec> items,
        int selectedIndex,
        string commandPrefix)
    {
        var selectedItem = items[selectedIndex];

        return new SlashSuggestionState
        {
            ShowSuggestions = true,
            ActiveLayer = SlashSuggestionLayer.CommandName,
            Items = items,
            SelectedIndex = selectedIndex,
            SelectedItem = selectedItem,
            GhostSuffix = CreateGhostSuffix(selectedItem, commandPrefix)
        };
    }

    private static string CreateGhostSuffix(SlashCommandSpec selectedItem, string commandPrefix)
    {
        if (!selectedItem.Name.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return selectedItem.Name[commandPrefix.Length..] + " ";
    }

    private static SlashSuggestionState Empty()
    {
        return new SlashSuggestionState
        {
            ShowSuggestions = false,
            ActiveLayer = SlashSuggestionLayer.None,
            Items = ImmutableArray<SlashCommandSpec>.Empty,
            SelectedIndex = -1,
            SelectedItem = null,
            GhostSuffix = string.Empty
        };
    }
}
