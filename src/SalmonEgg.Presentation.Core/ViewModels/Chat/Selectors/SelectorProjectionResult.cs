using System;
using System.Collections.Generic;

namespace SalmonEgg.Presentation.Core.ViewModels.Chat.Selectors;

public sealed record SelectorProjectionResult(
    IReadOnlyList<ComposerSelectorItemViewModel> DisplayItems,
    ComposerSelectorItemViewModel? SelectedDisplayItem,
    bool IsEnabled,
    bool IsSubmitBlocked,
    string? SubmitBlockReason,
    SelectorPlaceholderKind PlaceholderKind)
{
    public static SelectorProjectionResult Empty(ComposerSelectorKind kind)
        => new(
            Array.Empty<ComposerSelectorItemViewModel>(),
            null,
            IsEnabled: false,
            IsSubmitBlocked: false,
            SubmitBlockReason: null,
            SelectorPlaceholderKind.None);
}
