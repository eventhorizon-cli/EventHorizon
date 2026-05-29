namespace EventHorizon.Configuration;

public sealed class SaveAppConfigurationRequest
{
    public string? CurrentDefaultProvider { get; set; }

    public List<NamedProviderConfiguration> Providers { get; set; } = [];

    public List<McpServerOptions> McpServers { get; set; } = [];

    public SkillCatalogOptions Skills { get; set; } = new();
}

public sealed class NamedProviderConfiguration
{
    public string Name { get; set; } = string.Empty;

    public ProviderOptions Provider { get; set; } = new();
}

