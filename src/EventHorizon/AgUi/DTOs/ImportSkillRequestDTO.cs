using EventHorizon.Configuration;

namespace EventHorizon.AGUI.DTOs;

public sealed class ImportSkillRequestDTO
{
    public string Path { get; set; } = string.Empty;

    public string Target { get; set; } = "global";

    public string? SessionId { get; set; }
}

public sealed class SkillImportResponseDTO
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public ImportedSkillOptions? Skill { get; set; }

    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}

public sealed class SkillRemoveResponseDTO
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
