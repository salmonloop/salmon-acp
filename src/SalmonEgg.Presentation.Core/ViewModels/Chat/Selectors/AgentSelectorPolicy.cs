using System;
using System.Collections.Generic;
using System.Linq;
using SalmonEgg.Domain.Models;

namespace SalmonEgg.Presentation.Core.ViewModels.Chat.Selectors;

public sealed record AgentSelectorPolicyInput(
    string Identity,
    IReadOnlyList<ServerConfiguration> Agents,
    string? SelectedProfileId,
    bool IsConnecting,
    bool HasConnectionError,
    bool IsSelectionResolved,
    AgentSelectorPlaceholderLabels Labels);

public sealed class AgentSelectorPolicy
{
    public SelectorPolicyProjection Project(AgentSelectorPolicyInput input)
    {
        var realItems = (input.Agents ?? Array.Empty<ServerConfiguration>())
            .Where(static agent => !string.IsNullOrWhiteSpace(agent.Id))
            .Select(agent => ComposerSelectorItemViewModel.Real(
                ComposerSelectorKind.Agent,
                agent.Id,
                string.IsNullOrWhiteSpace(agent.Name) ? agent.Id : agent.Name,
                input.Identity))
            .ToArray();

        if (input.IsConnecting)
        {
            return WithTopPlaceholder(input, realItems, SelectorPlaceholderKind.Loading, input.Labels.Loading);
        }

        if (input.HasConnectionError)
        {
            return WithTopPlaceholder(input, realItems, SelectorPlaceholderKind.Error, input.Labels.Error);
        }

        if (!input.IsSelectionResolved)
        {
            return WithTopPlaceholder(input, realItems, SelectorPlaceholderKind.Unresolved, input.Labels.Unresolved);
        }

        if (realItems.Length == 0)
        {
            var placeholder = ComposerSelectorItemViewModel.Placeholder(
                ComposerSelectorKind.Agent,
                SelectorPlaceholderKind.Default,
                input.Labels.Empty,
                input.Identity,
                blocksSubmit: false);

            return new SelectorPolicyProjection(
                realItems,
                input.SelectedProfileId,
                placeholder,
                ReplaceSelectionWithPlaceholder: true,
                DisableRealItems: false,
                SelectorEnabled: false);
        }

        return new SelectorPolicyProjection(
            realItems,
            input.SelectedProfileId,
            Placeholder: null,
            ReplaceSelectionWithPlaceholder: false,
            DisableRealItems: false,
            SelectorEnabled: true);
    }

    private static SelectorPolicyProjection WithTopPlaceholder(
        AgentSelectorPolicyInput input,
        IReadOnlyList<ComposerSelectorItemViewModel> realItems,
        SelectorPlaceholderKind kind,
        string displayName)
    {
        var placeholder = ComposerSelectorItemViewModel.Placeholder(
            ComposerSelectorKind.Agent,
            kind,
            displayName,
            input.Identity,
            blocksSubmit: true);

        return new SelectorPolicyProjection(
            realItems,
            input.SelectedProfileId,
            placeholder,
            ReplaceSelectionWithPlaceholder: false,
            DisableRealItems: false,
            SelectorEnabled: true);
    }
}
