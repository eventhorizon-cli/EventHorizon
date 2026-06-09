namespace EventHorizon.Engine.Sessions;

public sealed class WorkspaceDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "workspace";
    public string WorkspaceRoot { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Configuration.SkillsOptions WorkspaceSkills { get; set; } = new();
    public List<string> SessionIds { get; set; } = [];
}
