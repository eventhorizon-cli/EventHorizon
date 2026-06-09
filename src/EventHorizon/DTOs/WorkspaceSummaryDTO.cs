namespace EventHorizon.DTOs;

public sealed record WorkspaceSummaryDTO(
    string Id,
    string Name,
    string WorkspaceRoot,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int SessionCount);
