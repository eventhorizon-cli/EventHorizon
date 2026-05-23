namespace EventHorizon.Terminal;

public sealed class TerminalCommandEntry
{
    public string CommandText { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

