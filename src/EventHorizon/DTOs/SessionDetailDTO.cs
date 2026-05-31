using EventHorizon.Configuration;

namespace EventHorizon.DTOs;

public sealed record SessionDetailDTO(
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
    string? WorkspaceRoot,
    SkillsOptions SessionSkills,
    IReadOnlyList<ChatMessageDTO> Messages);
