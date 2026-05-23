namespace EventHorizon.Terminal.Commands;

public sealed class TerminalCommandContext
{
    public TerminalCommandContext(TerminalRuntimeContext runtime, TerminalCommand command, CancellationToken cancellationToken)
    {
        Runtime = runtime;
        Command = command;
        CancellationToken = cancellationToken;
    }

    public TerminalRuntimeContext Runtime { get; }

    public TerminalCommand Command { get; }

    public CancellationToken CancellationToken { get; }
}

