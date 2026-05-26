namespace EventHorizon.Terminal.Commands;

public sealed class CancelCommand : ITerminalCommand
{
    public string Name => "/cancel";

    public string Description => "Cancel the active run";

    public Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
        => context.Dispatcher.CancelAsync();
}

