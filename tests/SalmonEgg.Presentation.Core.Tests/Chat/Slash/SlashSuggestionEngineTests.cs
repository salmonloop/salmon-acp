using SalmonEgg.Presentation.Core.Services.Chat.Slash;

namespace SalmonEgg.Presentation.Core.Tests.Chat.Slash;

public sealed class SlashSuggestionEngineTests
{
    [Fact]
    public void Evaluate_WhenEditingCommandName_ShowsMatchingCommandsAndGhostSuffix()
    {
        var registry = new SlashCommandRegistry(
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

        var result = SlashSuggestionEngine.Evaluate(
            SlashInputParser.Parse("/p"),
            registry);

        Assert.True(result.ShowSuggestions);
        Assert.Equal(SlashSuggestionLayer.CommandName, result.ActiveLayer);
        Assert.Equal(["plan", "prompt"], result.Items.Select(static item => item.Name).ToArray());
        Assert.Equal(0, result.SelectedIndex);
        Assert.Equal("plan", result.SelectedItem?.Name);
        Assert.Equal("lan ", result.GhostSuffix);
    }

    [Fact]
    public void Evaluate_WhenCommandPrefixHasNoMatches_HidesSuggestions()
    {
        var registry = new SlashCommandRegistry(
            localCommands: [],
            remoteCommands:
            [
                new SlashCommandSpec
                {
                    Name = "plan",
                    Description = "Planning command",
                    Source = SlashCommandSourceKind.Remote
                }
            ]);

        var result = SlashSuggestionEngine.Evaluate(
            SlashInputParser.Parse("/plaaan"),
            registry);

        Assert.False(result.ShowSuggestions);
        Assert.Equal(SlashSuggestionLayer.None, result.ActiveLayer);
        Assert.Empty(result.Items);
        Assert.Null(result.SelectedItem);
        Assert.Equal(string.Empty, result.GhostSuffix);
    }

    [Fact]
    public void Evaluate_WhenEditingArgumentTokenWithoutSubcommands_HidesTopLevelSuggestions()
    {
        var registry = new SlashCommandRegistry(
            localCommands: [],
            remoteCommands:
            [
                new SlashCommandSpec
                {
                    Name = "plan",
                    Description = "Planning command",
                    Source = SlashCommandSourceKind.Remote
                }
            ]);

        var result = SlashSuggestionEngine.Evaluate(
            SlashInputParser.Parse("/plan "),
            registry);

        Assert.False(result.ShowSuggestions);
        Assert.Equal(SlashSuggestionLayer.None, result.ActiveLayer);
        Assert.Empty(result.Items);
        Assert.Null(result.SelectedItem);
    }

    [Fact]
    public void Evaluate_WhenCommandMatchesByNonPrefixAlias_SuppressesGhostSuffix()
    {
        var registry = new SlashCommandRegistry(
            localCommands:
            [
                new SlashCommandSpec
                {
                    Name = "list",
                    Description = "List command",
                    Source = SlashCommandSourceKind.Local,
                    Aliases = ["ls"]
                }
            ],
            remoteCommands: []);

        var result = SlashSuggestionEngine.Evaluate(
            SlashInputParser.Parse("/ls"),
            registry);

        Assert.True(result.ShowSuggestions);
        Assert.Equal("list", result.SelectedItem?.Name);
        Assert.Equal(string.Empty, result.GhostSuffix);
    }
}
