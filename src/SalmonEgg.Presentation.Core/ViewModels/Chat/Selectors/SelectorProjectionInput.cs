using System.Collections.Generic;

namespace SalmonEgg.Presentation.Core.ViewModels.Chat.Selectors;

public sealed record SelectorProjectionInput(
    ComposerSelectorKind Kind,
    IReadOnlyList<ComposerSelectorItemViewModel> RealItems,
    string? SelectedSemanticValue,
    ComposerSelectorItemViewModel? Placeholder,
    bool ReplaceSelectionWithPlaceholder,
    bool DisableRealItems,
    bool SelectorEnabled);
