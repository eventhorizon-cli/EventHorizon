namespace EventHorizon.Engine.Sessions;

public sealed class SessionDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "session";
    public string Status { get; set; } = "idle";
    public string? ProviderName { get; set; }
    public string ProviderType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string WorkspaceRoot { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? LastRunId { get; set; }
    public string? Summary { get; set; }
    public int ChangedFilesCount { get; set; }
    public bool IsTitleGenerated { get; set; }
    public bool IsTitleManuallyEdited { get; set; }
    public string? SerializedSession { get; set; }
    public string? SessionId { get; set; }
    public Configuration.SkillsOptions SessionSkills { get; set; } = new();
    public List<SessionTranscriptEntry> Transcript { get; set; } = [];
    public SessionUsageSnapshot Usage { get; set; } = new();
}
