namespace EventHorizon.Terminal;

public sealed class TerminalMessage
{
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public bool IsStreamingPreview { get; set; }
}

