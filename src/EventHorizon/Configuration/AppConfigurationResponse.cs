namespace EventHorizon.Configuration;

public sealed class AppConfigurationResponse
{
    public string FilePath { get; set; } = string.Empty;

    public string? CurrentDefaultProvider { get; set; }

    public IReadOnlyList<ApiProviderViewModel> Providers { get; set; } = Array.Empty<ApiProviderViewModel>();

    public IReadOnlyList<McpServerOptions> McpServers { get; set; } = Array.Empty<McpServerOptions>();

    public SkillCatalogOptions Skills { get; set; } = new();
}

