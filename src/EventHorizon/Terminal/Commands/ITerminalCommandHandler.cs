namespace EventHorizon.Terminal.Commands;

public interface ITerminalCommandHandler
{
    bool CanHandle(TerminalCommand command);

    Task<TerminalCommandResult> ExecuteAsync(TerminalCommandContext context);
}

