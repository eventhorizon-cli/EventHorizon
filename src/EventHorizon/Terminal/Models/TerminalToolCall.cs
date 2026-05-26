namespace EventHorizon.Terminal.Models;

public enum TerminalToolCallStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Cancelled,
}

public sealed class TerminalToolCall
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public string Name { get; init; } = string.Empty;

    public string? Description { get; set; }

    public string? ArgumentsSummary { get; set; }

    public string? Output { get; set; }

    public string? Error { get; set; }

    public TerminalToolCallStatus Status { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? FinishedAt { get; set; }

    public TimeSpan? Duration => (FinishedAt ?? DateTimeOffset.UtcNow) - StartedAt;
}

