namespace EventHorizon.AGUI;

public sealed record AGUISessionDetail(
    string Id,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? LastRunId,
    string? Summary,
    int ChangedFilesCount,
    bool IsTitleGenerated,
    IReadOnlyList<AGUIChatMessage> Messages);

