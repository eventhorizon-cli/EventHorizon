namespace EventHorizon.Configuration;

public sealed class SkillsOptions
{
    public string? StoragePath { get; set; }

    public List<ImportedSkillOptions> Imported { get; set; } = [];
}
