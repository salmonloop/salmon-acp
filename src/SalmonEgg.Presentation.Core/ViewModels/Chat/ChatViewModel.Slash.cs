using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Immutable;
using SalmonEgg.Domain.Models.Conversation;
using SalmonEgg.Presentation.Core.Services.Chat.Slash;

namespace SalmonEgg.Presentation.ViewModels.Chat;

public partial class ChatViewModel
{
    private void ApplySlashCommandProjection(IReadOnlyList<ConversationAvailableCommandSnapshot> commands)
    {
        _slashCommandRegistry.ReplaceCommands(
            _localSlashCommandSource.GetCommands(),
            commands.Select(ToRemoteSlashCommandSpec).ToArray());

        UpdateSlashCommandCollection(AvailableSlashCommands, _slashCommandRegistry.Commands);
        RefreshSlashStateFromPrompt();
    }

    private void RefreshSlashStateFromPrompt()
    {
        var state = _slashInteractionCoordinator.UpdatePrompt(CurrentPrompt);
        ApplySlashSuggestionState(state);
    }

    private void ApplySlashSuggestionState(SlashSuggestionState state)
    {
        UpdateSlashCommandCollection(FilteredSlashCommands, state.Items);
        SelectedSlashCommand = state.ShowSuggestions
            && state.SelectedIndex >= 0
            && state.SelectedIndex < FilteredSlashCommands.Count
            ? FilteredSlashCommands[state.SelectedIndex]
            : null;
        ShowSlashCommands = state.ShowSuggestions;
        SlashGhostSuffix = state.GhostSuffix;
    }

    public bool TryAcceptSelectedSlashCommand(bool commitWithInputPlaceholder = false)
    {
        var result = _slashInteractionCoordinator.AcceptSelection();
        if (!result.Accepted)
        {
            return false;
        }

        CurrentPrompt = result.NextPromptText;
        return true;
    }

    public bool TryMoveSlashSelection(int delta)
    {
        if (!ShowSlashCommands || FilteredSlashCommands.Count == 0)
        {
            return false;
        }

        var state = _slashInteractionCoordinator.MoveSelection(delta);
        ApplySlashSuggestionState(state);
        return true;
    }

    private static void UpdateSlashCommandCollection(
        ObservableCollection<SlashCommandViewModel> current,
        IReadOnlyList<SlashCommandSpec> projected)
    {
        for (var index = 0; index < projected.Count; index++)
        {
            var projectedCommand = projected[index];
            if (index >= current.Count)
            {
                current.Add(CreateSlashCommandViewModel(projectedCommand));
                continue;
            }

            var existing = current[index];
            if (string.Equals(existing.Name, projectedCommand.Name, StringComparison.Ordinal)
                && string.Equals(existing.Description, projectedCommand.Description, StringComparison.Ordinal)
                && string.Equals(existing.InputHint, projectedCommand.InputHint, StringComparison.Ordinal))
            {
                continue;
            }

            existing.Name = projectedCommand.Name;
            existing.Description = projectedCommand.Description;
            existing.InputHint = projectedCommand.InputHint;
        }

        while (current.Count > projected.Count)
        {
            current.RemoveAt(current.Count - 1);
        }
    }

    private static SlashCommandViewModel CreateSlashCommandViewModel(SlashCommandSpec command)
        => new()
        {
            Name = command.Name,
            Description = command.Description,
            InputHint = command.InputHint
        };

    private static SlashCommandSpec ToRemoteSlashCommandSpec(ConversationAvailableCommandSnapshot command)
        => new()
        {
            Name = command.Name,
            Description = command.Description,
            Source = SlashCommandSourceKind.Remote,
            InputHint = command.InputHint
        };
}
