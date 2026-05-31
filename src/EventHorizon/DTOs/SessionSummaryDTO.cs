namespace EventHorizon.DTOs;

public sealed record SessionSummaryDTO(
    string Id,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ProviderName,
    string? ProviderType,
    string? Model,
    string? LastRunId,
    string? Summary,
    int ChangedFilesCount,
    bool IsTitleGenerated,
    string? WorkspaceRoot);
