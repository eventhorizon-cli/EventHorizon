using EventHorizon.Configuration;

namespace EventHorizon.AGUI.DTOs;

public sealed class SaveAppConfigurationRequestDTO
{
    public string? CurrentDefaultProvider { get; set; }

    public List<NamedProviderConfigurationDTO> Providers { get; set; } = [];

    public List<McpServerOptions> McpServers { get; set; } = [];

    public SkillsOptions Skills { get; set; } = new();
}

public sealed class NamedProviderConfigurationDTO
{
    public string Name { get; set; } = string.Empty;

    public ProviderOptions Provider { get; set; } = new();
}
