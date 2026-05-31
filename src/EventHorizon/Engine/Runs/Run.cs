namespace EventHorizon.Engine.Runs;

public sealed class Run
{
    public required string Id { get; init; }

    public required string ThreadId { get; init; }

    public required string SessionId { get; init; }

    public required string Task { get; init; }

    public string? WorkingDirectory { get; init; }

    public string? ProviderName { get; init; }

    public string? Model { get; init; }

    public string Status { get; private set; } = RunStates.Idle;

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
        Status = RunStates.Running;
        DetailedStatus = detailedStatus;
        Error = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted(string? detailedStatus = null)
    {
        Status = RunStates.Completed;
        DetailedStatus = detailedStatus;
        Error = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error, string? detailedStatus = null)
    {
        Status = RunStates.Failed;
        DetailedStatus = detailedStatus;
        Error = error;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCancelled(string? detailedStatus = null)
    {
        Status = RunStates.Cancelled;
        DetailedStatus = detailedStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
