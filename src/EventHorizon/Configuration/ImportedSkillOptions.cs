namespace EventHorizon.Configuration;

public sealed class ImportedSkillOptions
{
    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;
}

