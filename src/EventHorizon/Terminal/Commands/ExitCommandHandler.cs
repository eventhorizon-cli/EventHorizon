namespace EventHorizon.Terminal.Commands;

public sealed class ExitCommandHandler : ITerminalCommandHandler
{
    public bool CanHandle(TerminalCommand command) => command.Name is "/exit" or "/quit";

    public Task<TerminalCommandResult> ExecuteAsync(TerminalCommandContext context)
    {
        context.Runtime.State.AddActivity("exit", "User requested exit");
        return Task.FromResult(TerminalCommandResult.Exit("User requested exit."));
    }
}
