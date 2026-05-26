namespace EventHorizon.Terminal.Models;

public enum TerminalPlanItemStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped,
}

public sealed class TerminalPlanItem
{
    public string Title { get; set; } = string.Empty;

    public TerminalPlanItemStatus Status { get; set; }

    public string? Detail { get; set; }
}

