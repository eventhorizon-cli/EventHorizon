namespace EventHorizon.Engine.Sessions;

public sealed record SessionSummary(
    string Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ProviderName,
    string ProviderType,
    string Model,
    string Status,
    string? LastRunId,
    string? Summary,
    int ChangedFilesCount,
    bool IsTitleGenerated,
    string? WorkspaceRoot);
