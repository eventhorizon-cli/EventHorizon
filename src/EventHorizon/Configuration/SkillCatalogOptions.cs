namespace EventHorizon.Configuration;

public sealed class SkillCatalogOptions
{
    public string? StoragePath { get; set; }

    public List<ImportedSkillOptions> Imported { get; set; } = [];
}

