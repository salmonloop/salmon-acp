using System;
using System.Collections.Generic;
using System.Windows.Input;
using SalmonEgg.Presentation.Core.ViewModels.Chat.Selectors;

namespace SalmonEgg.Presentation.Core.ViewModels.Composer;

public sealed record ComposerSelectorSlotPresentation(
    bool IsVisible,
    bool IsEnabled,
    IReadOnlyList<ComposerSelectorItemViewModel> Items,
    ComposerSelectorItemViewModel? SelectedItem,
    ICommand? SelectionCommand)
{
    public static ComposerSelectorSlotPresentation Hidden()
        => new(
            IsVisible: false,
            IsEnabled: false,
            Items: Array.Empty<ComposerSelectorItemViewModel>(),
            SelectedItem: null,
            SelectionCommand: null);
}
