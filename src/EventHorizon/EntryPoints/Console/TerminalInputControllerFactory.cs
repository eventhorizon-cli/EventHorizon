using EventHorizon.Terminal;

namespace EventHorizon.EntryPoints.Console;

public sealed class TerminalInputControllerFactory : ITerminalInputControllerFactory
{
    public TerminalInputController Create(string workspaceRoot) => new(workspaceRoot);
}
