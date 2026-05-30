namespace EventHorizon.Configuration;

public interface IAppConfigurationService
{
    AppOptions GetAppOptions();

    ProvidersOptions GetProvidersOptions();

    McpOptions GetMcpOptions();

    SkillsOptions GetSkillsOptions();

    Task SaveAsync(
        ProvidersOptions providers,
        McpOptions mcp,
        SkillsOptions skills,
        CancellationToken cancellationToken);

    Task SetDefaultProviderAsync(string? providerName, CancellationToken cancellationToken);
}
