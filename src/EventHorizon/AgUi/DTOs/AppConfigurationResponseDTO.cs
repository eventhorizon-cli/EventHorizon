using EventHorizon.Configuration;

namespace EventHorizon.AGUI.DTOs;

public sealed class AppConfigurationResponseDTO
{
    public string FilePath { get; set; } = string.Empty;

    public string? CurrentDefaultProvider { get; set; }

    public IReadOnlyList<ApiProviderViewModel> Providers { get; set; } = Array.Empty<ApiProviderViewModel>();

    public IReadOnlyList<McpServerOptions> McpServers { get; set; } = Array.Empty<McpServerOptions>();

    public SkillsOptions Skills { get; set; } = new();
}
