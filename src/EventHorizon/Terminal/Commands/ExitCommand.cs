namespace EventHorizon.Terminal.Commands;

public sealed class ExitCommand : ITerminalCommand
{
    public string Name => "/exit";

    public string Description => "Exit the terminal workbench";

    public Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
        => context.Dispatcher.RequestExitAsync(cancellationToken);
}

