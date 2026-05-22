using System;
using System.Collections.Generic;
using System.Linq;

namespace SalmonEgg.Presentation.Core.ViewModels.Chat.Selectors;

public sealed class SelectorProjectionPresenter
{
    public SelectorProjectionResult Present(SelectorProjectionInput input)
    {
        var realItems = input.RealItems ?? Array.Empty<ComposerSelectorItemViewModel>();
        var projectedRealItems = input.DisableRealItems
            ? realItems.Select(static item => item.AsDisabled()).ToArray()
            : realItems.ToArray();

        var displayItems = input.Placeholder is null
            ? projectedRealItems
            : new[] { input.Placeholder }.Concat(projectedRealItems).ToArray();

        var selected = ResolveSelectedDisplayItem(input, projectedRealItems);
        var isSubmitBlocked = input.Placeholder?.BlocksSubmit == true;
        var submitBlockReason = isSubmitBlocked
            ? input.Placeholder!.DisplayName
            : null;

        return new SelectorProjectionResult(
            displayItems,
            selected,
            input.SelectorEnabled,
            isSubmitBlocked,
            submitBlockReason,
            input.Placeholder?.PlaceholderKind ?? SelectorPlaceholderKind.None);
    }

    private static ComposerSelectorItemViewModel? ResolveSelectedDisplayItem(
        SelectorProjectionInput input,
        IReadOnlyList<ComposerSelectorItemViewModel> projectedRealItems)
    {
        if (input.Placeholder is not null && input.ReplaceSelectionWithPlaceholder)
        {
            return input.Placeholder;
        }

        if (string.IsNullOrWhiteSpace(input.SelectedSemanticValue))
        {
            return input.Placeholder;
        }

        return projectedRealItems.FirstOrDefault(item =>
            string.Equals(item.SemanticValue, input.SelectedSemanticValue, StringComparison.Ordinal));
    }
}
