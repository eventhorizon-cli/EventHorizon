namespace EventHorizon.Terminal.Commands;

public sealed class TerminalCommandDispatcher
{
    private readonly IReadOnlyList<ITerminalCommandHandler> _handlers;

    public TerminalCommandDispatcher(IEnumerable<ITerminalCommandHandler> handlers)
    {
        _handlers = handlers.ToList();
    }

    public async Task<TerminalCommandResult> DispatchAsync(string input, TerminalRuntimeContext runtime, CancellationToken cancellationToken)
    {
        TerminalCommand command = TerminalCommand.Parse(input);
        if (!command.IsSlashCommand)
        {
            return TerminalCommandResult.NotHandled();
        }

        foreach (ITerminalCommandHandler handler in _handlers)
        {
            if (handler.CanHandle(command))
            {
                return await handler.ExecuteAsync(new TerminalCommandContext(runtime, command, cancellationToken)).ConfigureAwait(false);
            }
        }

        runtime.State.AddActivity("command", "Unknown command", input);
        return TerminalCommandResult.Success($"Unknown command '{command.Name}'. Try /help.");
    }
}

