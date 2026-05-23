namespace EventHorizon.Terminal;

public sealed class TerminalActivityEntry
{
    public string Kind { get; set; } = "info";
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

