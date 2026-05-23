namespace EventHorizon.Terminal;

public sealed class TerminalErrorEntry
{
    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string ExceptionType { get; set; } = string.Empty;

    public string LogFilePath { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

