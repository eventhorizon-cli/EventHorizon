namespace EventHorizon.Configuration;

public sealed class ImportSkillRequest
{
    public string Path { get; set; } = string.Empty;
}

public sealed class SkillImportResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public ImportedSkillOptions? Skill { get; set; }

    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}

