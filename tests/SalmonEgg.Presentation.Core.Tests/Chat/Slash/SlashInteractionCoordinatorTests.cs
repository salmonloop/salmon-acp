using SalmonEgg.Presentation.Core.Services.Chat.Slash;

namespace SalmonEgg.Presentation.Core.Tests.Chat.Slash;

public sealed class SlashInteractionCoordinatorTests
{
    [Fact]
    public void MoveSelection_WhenSuggestionsAreVisible_ClampsAndReselectsMatchingItem()
    {
        var coordinator = new SlashInteractionCoordinator(CreateRegistry());

        coordinator.UpdatePrompt("/p");

        var movedState = coordinator.MoveSelection(1);
        var clampedState = coordinator.MoveSelection(10);

        Assert.Equal("prompt", movedState.SelectedItem?.Name);
        Assert.Equal("prompt", clampedState.SelectedItem?.Name);
        Assert.Equal("rompt ", clampedState.GhostSuffix);
    }

    [Fact]
    public void AcceptSelection_WhenSelectionExists_RewritesPromptToCommandHeadWithTrailingSpace()
    {
        var coordinator = new SlashInteractionCoordinator(CreateRegistry());

        coordinator.UpdatePrompt("  /pr");

        var result = coordinator.AcceptSelection();

        Assert.True(result.Accepted);
        Assert.Equal("  /prompt ", result.NextPromptText);
        Assert.False(result.NextSuggestionState.ShowSuggestions);
    }

    [Fact]
    public void AcceptSelection_WhenNoSuggestionIsActive_KeepsPromptUnchanged()
    {
        var coordinator = new SlashInteractionCoordinator(CreateRegistry());
        coordinator.UpdatePrompt("/plaaan");

        var result = coordinator.AcceptSelection();

        Assert.False(result.Accepted);
        Assert.Equal("/plaaan", result.NextPromptText);
        Assert.False(result.NextSuggestionState.ShowSuggestions);
    }

    private static SlashCommandRegistry CreateRegistry()
    {
        return new SlashCommandRegistry(
            localCommands: [],
            remoteCommands:
            [
                new SlashCommandSpec
                {
                    Name = "plan",
                    Description = "Planning command",
                    Source = SlashCommandSourceKind.Remote
                },
                new SlashCommandSpec
                {
                    Name = "prompt",
                    Description = "Prompt command",
                    Source = SlashCommandSourceKind.Remote
                }
            ]);
    }
}
