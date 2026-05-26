using EventHorizon.Terminal;

namespace EventHorizon.EntryPoints.Console;

public sealed class TerminalWorkbenchHost
{
    private readonly TerminalApp _terminalApp;

    public TerminalWorkbenchHost(TerminalApp terminalApp)
    {
        _terminalApp = terminalApp;
    }

    public Task RunAsync(CancellationToken cancellationToken)
        => _terminalApp.RunAsync(cancellationToken);
}
