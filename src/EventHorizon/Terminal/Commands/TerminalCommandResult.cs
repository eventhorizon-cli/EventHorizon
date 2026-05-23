namespace EventHorizon.Terminal.Commands;

public sealed record TerminalCommandResult(bool Handled, string StatusMessage, bool ShouldExit = false)
{
    public static TerminalCommandResult NotHandled() => new(false, string.Empty);

    public static TerminalCommandResult Success(string statusMessage) => new(true, statusMessage);

    public static TerminalCommandResult Exit(string statusMessage) => new(true, statusMessage, true);
}

