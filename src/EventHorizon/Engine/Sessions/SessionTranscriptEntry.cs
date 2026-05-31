namespace EventHorizon.Engine.Sessions;

public sealed class SessionTranscriptEntry
{
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
