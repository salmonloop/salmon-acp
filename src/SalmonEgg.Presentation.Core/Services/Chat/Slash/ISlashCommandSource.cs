namespace SalmonEgg.Presentation.Core.Services.Chat.Slash;

public interface ISlashCommandSource
{
    IReadOnlyList<SlashCommandSpec> GetCommands();
}
