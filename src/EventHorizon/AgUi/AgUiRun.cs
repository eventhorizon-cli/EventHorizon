namespace EventHorizon.AGUI;

public sealed class AGUIRun
{
    public required string Id { get; init; }

    public required string ThreadId { get; init; }

    public string? SessionId { get; init; }

    public required string Task { get; init; }

    public string? WorkingDirectory { get; init; }

    public string? ProviderName { get; init; }

    public string? Model { get; init; }

    public string Status { get; private set; } = AGUIRunStates.Idle;

    public string? DetailedStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string? Error { get; private set; }

    public void SetDetailedStatus(string? detailedStatus)
    {
        DetailedStatus = detailedStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRunning(string? detailedStatus = null)
    {
        Status = AGUIRunStates.Running;
        DetailedStatus = detailedStatus;
        Error = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted(string? detailedStatus = null)
    {
        Status = AGUIRunStates.Completed;
        DetailedStatus = detailedStatus;
        Error = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error, string? detailedStatus = null)
    {
        Status = AGUIRunStates.Failed;
        DetailedStatus = detailedStatus;
        Error = error;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCancelled(string? detailedStatus = null)
    {
        Status = AGUIRunStates.Cancelled;
        DetailedStatus = detailedStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
