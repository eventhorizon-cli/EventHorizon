namespace EventHorizon.Conversations;

public sealed class ConversationSessionDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "session";
    public string ProviderType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string WorkspaceRoot { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? SerializedSession { get; set; }
    public string? ConversationId { get; set; }
    public List<ConversationTranscriptEntry> Transcript { get; set; } = [];
    public ConversationUsageSnapshot Usage { get; set; } = new();
}
