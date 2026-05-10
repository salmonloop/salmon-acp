using System.Collections.Immutable;
using SalmonEgg.Presentation.Core.Services.Chat.Slash;

namespace SalmonEgg.Presentation.Core.Tests.Chat.Slash;

public sealed class SlashCommandRegistryTests
{
    [Fact]
    public void FindCommandSuggestions_WhenLocalAndRemoteNamesConflict_LocalCommandWins()
    {
        var registry = new SlashCommandRegistry(
            localCommands:
            [
                new SlashCommandSpec
                {
                    Name = "help",
                    Description = "Local help",
                    Source = SlashCommandSourceKind.Local
                }
            ],
            remoteCommands:
            [
                new SlashCommandSpec
                {
                    Name = "help",
                    Description = "Remote help",
                    Source = SlashCommandSourceKind.Remote
                }
            ]);

        var command = Assert.Single(registry.FindCommandSuggestions("he"));

        Assert.Equal("help", command.Name);
        Assert.Equal("Local help", command.Description);
        Assert.Equal(SlashCommandSourceKind.Local, command.Source);
    }

    [Fact]
    public void FindCommandSuggestions_WhenPrefixMatchesAlias_ReturnsCanonicalAuthoritativeCommand()
    {
        var registry = new SlashCommandRegistry(
            localCommands:
            [
                new SlashCommandSpec
                {
                    Name = "prompt",
                    Description = "Prompt command",
                    Source = SlashCommandSourceKind.Local,
                    Aliases = ImmutableArray.Create("pr")
                }
            ],
            remoteCommands: []);

        var command = Assert.Single(registry.FindCommandSuggestions("pr"));

        Assert.Equal("prompt", command.Name);
        Assert.Equal(SlashCommandSourceKind.Local, command.Source);
    }

    [Fact]
    public void TryResolve_WhenLocalAliasConflictsWithRemoteAlias_LocalCommandWins()
    {
        var registry = new SlashCommandRegistry(
            localCommands:
            [
                new SlashCommandSpec
                {
                    Name = "help",
                    Description = "Local help",
                    Source = SlashCommandSourceKind.Local,
                    Aliases = ImmutableArray.Create("h")
                }
            ],
            remoteCommands:
            [
                new SlashCommandSpec
                {
                    Name = "history",
                    Description = "Remote history",
                    Source = SlashCommandSourceKind.Remote,
                    Aliases = ImmutableArray.Create("h")
                }
            ]);

        var resolved = registry.TryResolve("h", out var command);

        Assert.True(resolved);
        Assert.NotNull(command);
        Assert.Equal("help", command.Name);
        Assert.Equal(SlashCommandSourceKind.Local, command.Source);
    }

    [Fact]
    public void TryResolve_WhenRemoteAliasSortsEarlierThanLocalAlias_LocalCommandStillWins()
    {
        var registry = new SlashCommandRegistry(
            localCommands:
            [
                new SlashCommandSpec
                {
                    Name = "zhelp",
                    Description = "Local help",
                    Source = SlashCommandSourceKind.Local,
                    Aliases = ImmutableArray.Create("h")
                }
            ],
            remoteCommands:
            [
                new SlashCommandSpec
                {
                    Name = "aardvark",
                    Description = "Remote earlier name",
                    Source = SlashCommandSourceKind.Remote,
                    Aliases = ImmutableArray.Create("h")
                }
            ]);

        var resolved = registry.TryResolve("h", out var command);

        Assert.True(resolved);
        Assert.NotNull(command);
        Assert.Equal("zhelp", command.Name);
        Assert.Equal(SlashCommandSourceKind.Local, command.Source);
    }

    [Fact]
    public void FindCommandSuggestions_WhenCommandIsHidden_ExcludesItFromSuggestions()
    {
        var registry = new SlashCommandRegistry(
            localCommands:
            [
                new SlashCommandSpec
                {
                    Name = "plan",
                    Description = "Plan",
                    Source = SlashCommandSourceKind.Local,
                    Hidden = true
                }
            ],
            remoteCommands: []);

        var commands = registry.FindCommandSuggestions("pl");

        Assert.Empty(commands);
    }
}
