using EventHorizon.Terminal;

namespace EventHorizon.EntryPoints.Console;

public interface ITerminalInputControllerFactory
{
    TerminalInputController Create(string workspaceRoot);
}
