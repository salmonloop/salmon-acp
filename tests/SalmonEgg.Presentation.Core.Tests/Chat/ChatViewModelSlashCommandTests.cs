using System.Linq;
using SalmonEgg.Domain.Models.Conversation;
using SalmonEgg.Presentation.ViewModels.Chat;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Chat;

public partial class ChatViewModelTests
{
    [Fact]
    public async Task CurrentPrompt_WhenSlashPrefixEntered_FiltersCommandsAndSelectsFirstMatch()
    {
        await using var fixture = await CreateSlashCommandViewModelAsync();
        var viewModel = fixture.ViewModel;

        viewModel.AvailableSlashCommands.Add(new SlashCommandViewModel
        {
            Name = "plan",
            Description = "Planning command",
            InputHint = "goal"
        });
        viewModel.AvailableSlashCommands.Add(new SlashCommandViewModel
        {
            Name = "prompt",
            Description = "Prompt command",
            InputHint = "text"
        });
        viewModel.AvailableSlashCommands.Add(new SlashCommandViewModel
        {
            Name = "status",
            Description = "Status command",
            InputHint = "scope"
        });

        viewModel.CurrentPrompt = "/p";

        Assert.True(viewModel.ShowSlashCommands);
        Assert.Equal(new[] { "plan", "prompt" }, viewModel.FilteredSlashCommands.Select(static command => command.Name).ToArray());
        Assert.Equal("plan", viewModel.SelectedSlashCommand?.Name);
        Assert.Equal("lan ", viewModel.SlashGhostSuffix);
    }

    [Fact]
    public async Task TryMoveSlashSelection_WhenVisible_UpdatesSelectionWithinBounds()
    {
        await using var fixture = await CreateSlashCommandViewModelAsync();
        var viewModel = fixture.ViewModel;

        viewModel.AvailableSlashCommands.Add(new SlashCommandViewModel
        {
            Name = "plan",
            Description = "Planning command",
            InputHint = "goal"
        });
        viewModel.AvailableSlashCommands.Add(new SlashCommandViewModel
        {
            Name = "prompt",
            Description = "Prompt command",
            InputHint = "text"
        });
        viewModel.CurrentPrompt = "/p";

        Assert.True(viewModel.TryMoveSlashSelection(1));
        Assert.Equal("prompt", viewModel.SelectedSlashCommand?.Name);
        Assert.Equal("rompt ", viewModel.SlashGhostSuffix);

        Assert.True(viewModel.TryMoveSlashSelection(1));
        Assert.Equal("prompt", viewModel.SelectedSlashCommand?.Name);

        Assert.True(viewModel.TryMoveSlashSelection(-5));
        Assert.Equal("plan", viewModel.SelectedSlashCommand?.Name);
    }

    [Fact]
    public async Task TryAcceptSelectedSlashCommand_WhenSelectionExists_CompletesPromptAndClosesMenu()
    {
        await using var fixture = await CreateSlashCommandViewModelAsync();
        var viewModel = fixture.ViewModel;

        viewModel.AvailableSlashCommands.Add(new SlashCommandViewModel
        {
            Name = "plan",
            Description = "Planning command",
            InputHint = "goal"
        });
        viewModel.AvailableSlashCommands.Add(new SlashCommandViewModel
        {
            Name = "prompt",
            Description = "Prompt command",
            InputHint = "text"
        });
        viewModel.CurrentPrompt = "   /p next step";
        Assert.True(viewModel.TryMoveSlashSelection(1));

        var accepted = viewModel.TryAcceptSelectedSlashCommand();

        Assert.True(accepted);
        Assert.Equal("   /prompt next step", viewModel.CurrentPrompt);
        Assert.False(viewModel.ShowSlashCommands);
        Assert.Equal(string.Empty, viewModel.SlashGhostSuffix);
    }

    [Fact]
    public async Task CurrentPrompt_WhenNotSlashPrefix_HidesCommandsAndClearsSelection()
    {
        await using var fixture = await CreateSlashCommandViewModelAsync();
        var viewModel = fixture.ViewModel;

        viewModel.AvailableSlashCommands.Add(new SlashCommandViewModel
        {
            Name = "plan",
            Description = "Planning command",
            InputHint = "goal"
        });
        viewModel.CurrentPrompt = "/p";
        Assert.True(viewModel.ShowSlashCommands);
        Assert.NotNull(viewModel.SelectedSlashCommand);

        viewModel.CurrentPrompt = "plain text";

        Assert.False(viewModel.ShowSlashCommands);
        Assert.Null(viewModel.SelectedSlashCommand);
        Assert.Empty(viewModel.FilteredSlashCommands);
        Assert.Equal(string.Empty, viewModel.SlashGhostSuffix);
    }

    private static async Task<ViewModelFixture> CreateSlashCommandViewModelAsync()
    {
        var syncContext = new QueueingSynchronizationContext();
        var fixture = CreateViewModel(syncContext);

        await WaitForConditionAsync(() =>
        {
            syncContext.RunAll();
            return Task.FromResult(fixture.Preferences.IsLoaded);
        });

        return fixture;
    }
}
