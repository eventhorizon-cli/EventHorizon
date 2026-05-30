using EventHorizon.Configuration;

namespace EventHorizon.AGUI.DTOs;

public sealed record AGUISessionDetailDTO(
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
    IReadOnlyList<AGUIChatMessageDTO> Messages);
