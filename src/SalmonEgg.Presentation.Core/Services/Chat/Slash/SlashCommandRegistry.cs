namespace SalmonEgg.Presentation.Core.Services.Chat.Slash;

public sealed class SlashCommandRegistry
{
    private readonly Dictionary<string, SlashCommandSpec> _commandsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SlashCommandSpec> _commandsByAlias = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<SlashCommandSpec> _authoritativeCommands = [];

    public SlashCommandRegistry(
        IReadOnlyList<SlashCommandSpec>? localCommands = null,
        IReadOnlyList<SlashCommandSpec>? remoteCommands = null)
    {
        ReplaceCommands(
            localCommands ?? [],
            remoteCommands ?? []);
    }

    public IReadOnlyList<SlashCommandSpec> Commands => _authoritativeCommands;

    public void ReplaceCommands(
        IReadOnlyList<SlashCommandSpec> localCommands,
        IReadOnlyList<SlashCommandSpec> remoteCommands)
    {
        var authoritativeByName = new Dictionary<string, SlashCommandSpec>(StringComparer.OrdinalIgnoreCase);

        AddAuthoritativeCommands(authoritativeByName, remoteCommands);
        AddAuthoritativeCommands(authoritativeByName, localCommands);

        _authoritativeCommands = authoritativeByName.Values
            .OrderBy(static command => command.Order)
            .ThenBy(static command => command.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _commandsByName.Clear();
        _commandsByAlias.Clear();

        foreach (var command in _authoritativeCommands)
        {
            _commandsByName[command.Name] = command;
        }

        RegisterAliases(remoteCommands, authoritativeByName, overwriteExisting: false);
        RegisterAliases(localCommands, authoritativeByName, overwriteExisting: true);
    }

    public IReadOnlyList<SlashCommandSpec> FindCommandSuggestions(string? prefix)
    {
        var normalizedPrefix = prefix ?? string.Empty;

        return _authoritativeCommands
            .Where(static command => !command.Hidden)
            .Where(command => CommandMatchesPrefix(command, normalizedPrefix))
            .ToArray();
    }

    public bool TryResolve(string? nameOrAlias, out SlashCommandSpec? command)
    {
        command = null;

        if (string.IsNullOrWhiteSpace(nameOrAlias))
        {
            return false;
        }

        if (_commandsByName.TryGetValue(nameOrAlias, out command))
        {
            return true;
        }

        return _commandsByAlias.TryGetValue(nameOrAlias, out command);
    }

    private static void AddAuthoritativeCommands(
        IDictionary<string, SlashCommandSpec> authoritativeByName,
        IReadOnlyList<SlashCommandSpec> commands)
    {
        foreach (var command in commands)
        {
            authoritativeByName[command.Name] = command;
        }
    }

    private bool CommandMatchesPrefix(SlashCommandSpec command, string prefix)
    {
        if (command.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return command.Aliases.Any(alias =>
            alias.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && _commandsByAlias.TryGetValue(alias, out var authoritativeCommand)
            && ReferenceEquals(authoritativeCommand, command));
    }

    private void RegisterAliases(
        IReadOnlyList<SlashCommandSpec> commands,
        IReadOnlyDictionary<string, SlashCommandSpec> authoritativeByName,
        bool overwriteExisting)
    {
        foreach (var command in commands)
        {
            if (!authoritativeByName.TryGetValue(command.Name, out var authoritativeCommand)
                || !ReferenceEquals(authoritativeCommand, command))
            {
                continue;
            }

            foreach (var alias in command.Aliases)
            {
                if (overwriteExisting || !_commandsByAlias.ContainsKey(alias))
                {
                    _commandsByAlias[alias] = command;
                }
            }
        }
    }
}
