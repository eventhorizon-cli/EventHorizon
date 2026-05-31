namespace EventHorizon.Configuration;

public interface IAppConfigurationService
{
    ProvidersOptions GetProvidersOptions();

    McpOptions GetMcpOptions();

    SkillsOptions GetSkillsOptions();

    void Save(
        ProvidersOptions providers,
        McpOptions mcp,
        SkillsOptions skills,
        CancellationToken cancellationToken);

    void SetDefaultProvider(string? providerName, CancellationToken cancellationToken);
}
