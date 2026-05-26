namespace EventHorizon.Terminal.Commands;

public sealed class SessionCommand : ITerminalCommand
{
    public string Name => "/session";

    public string Description => "Open or restore sessions";

    public Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
        => context.Dispatcher.ShowSessionsAsync(cancellationToken);
}

