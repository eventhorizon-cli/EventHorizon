namespace EventHorizon.Terminal.Commands;

public interface ITerminalCommand
{
    string Name { get; }

    string Description { get; }

    Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken);
}

