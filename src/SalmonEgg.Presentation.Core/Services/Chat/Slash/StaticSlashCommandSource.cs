using System.Collections.Immutable;

namespace SalmonEgg.Presentation.Core.Services.Chat.Slash;

public sealed class StaticSlashCommandSource : ISlashCommandSource
{
    public static StaticSlashCommandSource Empty { get; } = new([]);

    private readonly ImmutableArray<SlashCommandSpec> _commands;

    public StaticSlashCommandSource(IReadOnlyList<SlashCommandSpec> commands)
    {
        _commands = commands.ToImmutableArray();
    }

    public IReadOnlyList<SlashCommandSpec> GetCommands() => _commands;
}
