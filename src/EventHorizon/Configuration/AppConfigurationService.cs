using Microsoft.Extensions.Options;

namespace EventHorizon.Configuration;

internal sealed class AppConfigurationService : IAppConfigurationService
{
    private readonly AppOptions _appOptions;
    private readonly ProvidersOptions _providersOptions;
    private readonly McpOptions _mcpOptions;
    private readonly SkillsOptions _skillsOptions;
    private readonly IOptionsNormalizer _normalizer;
    private readonly IUserConfigurationFileService _userConfigurationFileService;
    private readonly IUserProvidersFileService _userProvidersFileService;
    private readonly IUserMcpFileService _userMcpFileService;
    private readonly IUserSkillsFileService _userSkillsFileService;

    public AppConfigurationService(
        IOptions<AppOptions> appOptions,
        IOptions<ProvidersOptions> providersOptions,
        IOptions<McpOptions> mcpOptions,
        IOptions<SkillsOptions> skillsOptions,
        IOptionsNormalizer normalizer,
        IUserConfigurationFileService userConfigurationFileService,
        IUserProvidersFileService userProvidersFileService,
        IUserMcpFileService userMcpFileService,
        IUserSkillsFileService userSkillsFileService)
    {
        _appOptions = appOptions.Value;
        _providersOptions = providersOptions.Value;
        _mcpOptions = mcpOptions.Value;
        _skillsOptions = skillsOptions.Value;
        _normalizer = normalizer;
        _userConfigurationFileService = userConfigurationFileService;
        _userProvidersFileService = userProvidersFileService;
        _userMcpFileService = userMcpFileService;
        _userSkillsFileService = userSkillsFileService;
    }

    public AppOptions GetAppOptions()
        => _appOptions;

    public ProvidersOptions GetProvidersOptions()
        => _providersOptions;

    public McpOptions GetMcpOptions()
        => _mcpOptions;

    public SkillsOptions GetSkillsOptions()
        => _skillsOptions;

    public Task SaveAsync(
        ProvidersOptions providers,
        McpOptions mcp,
        SkillsOptions skills,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CopyProvidersInto(_providersOptions, providers);
        CopyMcpInto(_mcpOptions, mcp);
        CopySkillsInto(_skillsOptions, skills);

        _normalizer.NormalizeProviders(_providersOptions);
        _normalizer.NormalizeMcp(_mcpOptions);
        _normalizer.NormalizeSkills(_skillsOptions);

        _userConfigurationFileService.Save(_appOptions);
        _userProvidersFileService.Save(_providersOptions);
        _userMcpFileService.Save(_mcpOptions);
        _userSkillsFileService.Save(_skillsOptions);

        return Task.CompletedTask;
    }

    public Task SetDefaultProviderAsync(string? providerName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _providersOptions.CurrentDefaultProvider = string.IsNullOrWhiteSpace(providerName) ? null : providerName.Trim();
        _normalizer.NormalizeProviders(_providersOptions);
        _userProvidersFileService.Save(_providersOptions);
        return Task.CompletedTask;
    }

    private static void CopyProvidersInto(ProvidersOptions target, ProvidersOptions source)
    {
        target.CurrentDefaultProvider = source.CurrentDefaultProvider;
        target.Providers = source.Providers.ToDictionary(
            static pair => pair.Key,
            pair => MergeProvider(pair.Value, target.Providers.TryGetValue(pair.Key, out var existingProvider) ? existingProvider : null),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void CopyMcpInto(McpOptions target, McpOptions source)
    {
        target.Servers = [.. source.Servers.Select(CloneMcpServer)];
    }

    private static void CopySkillsInto(SkillsOptions target, SkillsOptions source)
    {
        target.StoragePath = source.StoragePath;
        target.Imported = [.. source.Imported.Select(CloneSkill)];
    }

    private static ProviderOptions MergeProvider(ProviderOptions source, ProviderOptions? existing)
        => new()
        {
            Name = source.Name,
            Type = source.Type,
            Model = source.Model,
            Models = [.. source.Models],
            ApiKey = string.IsNullOrWhiteSpace(source.ApiKey) ? existing?.ApiKey : source.ApiKey,
            Endpoint = source.Endpoint,
            Deployment = source.Deployment,
            UseDefaultAzureCredential = source.UseDefaultAzureCredential,
        };

    private static McpServerOptions CloneMcpServer(McpServerOptions server)
        => new()
        {
            Name = server.Name,
            Command = server.Command,
            Arguments = [.. server.Arguments],
            Url = server.Url,
            EnvironmentVariables = new Dictionary<string, string>(server.EnvironmentVariables, StringComparer.OrdinalIgnoreCase),
            Enabled = server.Enabled,
        };

    private static ImportedSkillOptions CloneSkill(ImportedSkillOptions skill)
        => new()
        {
            Name = skill.Name,
            Path = skill.Path,
            Description = skill.Description,
            ImportedAt = skill.ImportedAt,
        };
}
