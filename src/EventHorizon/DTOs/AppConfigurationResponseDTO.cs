using EventHorizon.Configuration;

namespace EventHorizon.DTOs;

public sealed class AppConfigurationResponseDTO
{
    public string FilePath { get; set; } = string.Empty;

    public string? CurrentDefaultProvider { get; set; }

    public IReadOnlyList<ApiProviderViewModelDTO> Providers { get; set; } = Array.Empty<ApiProviderViewModelDTO>();

    public IReadOnlyList<McpServerOptions> McpServers { get; set; } = Array.Empty<McpServerOptions>();

    public SkillsOptions Skills { get; set; } = new();
}
