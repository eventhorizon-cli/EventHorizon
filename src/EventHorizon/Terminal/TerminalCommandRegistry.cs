using EventHorizon.Terminal.Commands;

namespace EventHorizon.Terminal;

public sealed class TerminalCommandRegistry
{
    private readonly IReadOnlyDictionary<string, ITerminalCommand> _commands;

    public TerminalCommandRegistry(IEnumerable<ITerminalCommand> commands)
    {
        _commands = commands.ToDictionary(static command => command.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ITerminalCommand> All => _commands.Values.OrderBy(static command => command.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public bool TryGet(string name, out ITerminalCommand? command)
        => _commands.TryGetValue(name, out command);
}

