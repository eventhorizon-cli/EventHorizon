using Microsoft.Extensions.Options;

namespace EventHorizon.Configuration;

internal sealed class AppConfigurationService : IAppConfigurationService
{
    private readonly IOptionsMonitor<AgentOptions> _agentOptionsMonitor;
    private readonly IOptionsMonitor<PricingOptions> _pricingOptionsMonitor;
    private readonly IOptionsMonitor<ProvidersOptions> _providersOptionsMonitor;
    private readonly IOptionsMonitor<McpOptions> _mcpOptionsMonitor;
    private readonly IOptionsMonitor<SkillsOptions> _skillsOptionsMonitor;
    private readonly IOptionsNormalizer _normalizer;
    private readonly IUserConfigurationFileService _userConfigurationFileService;
    private readonly IUserProvidersFileService _userProvidersFileService;
    private readonly IUserMcpFileService _userMcpFileService;
    private readonly IUserSkillsFileService _userSkillsFileService;

    public AppConfigurationService(
        IOptionsMonitor<AgentOptions> agentOptionsMonitor,
        IOptionsMonitor<PricingOptions> pricingOptionsMonitor,
        IOptionsMonitor<ProvidersOptions> providersOptionsMonitor,
        IOptionsMonitor<McpOptions> mcpOptionsMonitor,
        IOptionsMonitor<SkillsOptions> skillsOptionsMonitor,
        IOptionsNormalizer normalizer,
        IUserConfigurationFileService userConfigurationFileService,
        IUserProvidersFileService userProvidersFileService,
        IUserMcpFileService userMcpFileService,
        IUserSkillsFileService userSkillsFileService)
    {
        _agentOptionsMonitor = agentOptionsMonitor;
        _pricingOptionsMonitor = pricingOptionsMonitor;
        _providersOptionsMonitor = providersOptionsMonitor;
        _mcpOptionsMonitor = mcpOptionsMonitor;
        _skillsOptionsMonitor = skillsOptionsMonitor;
        _normalizer = normalizer;
        _userConfigurationFileService = userConfigurationFileService;
        _userProvidersFileService = userProvidersFileService;
        _userMcpFileService = userMcpFileService;
        _userSkillsFileService = userSkillsFileService;
    }

    public ProvidersOptions GetProvidersOptions()
        => _providersOptionsMonitor.CurrentValue;

    public McpOptions GetMcpOptions()
        => _mcpOptionsMonitor.CurrentValue;

    public SkillsOptions GetSkillsOptions()
        => _skillsOptionsMonitor.CurrentValue;

    public void Save(
        ProvidersOptions providers,
        McpOptions mcp,
        SkillsOptions skills,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var providersOptions = _providersOptionsMonitor.CurrentValue;
        var mcpOptions = _mcpOptionsMonitor.CurrentValue;
        var skillsOptions = _skillsOptionsMonitor.CurrentValue;

        CopyProvidersInto(providersOptions, providers);
        CopyMcpInto(mcpOptions, mcp);
        CopySkillsInto(skillsOptions, skills);

        _normalizer.NormalizeProviders(providersOptions);
        _normalizer.NormalizeMcp(mcpOptions);
        _normalizer.NormalizeSkills(skillsOptions);

        _userConfigurationFileService.Save(_agentOptionsMonitor.CurrentValue, _pricingOptionsMonitor.CurrentValue);
        _userProvidersFileService.Save(providersOptions);
        _userMcpFileService.Save(mcpOptions);
        _userSkillsFileService.Save(skillsOptions);
    }

    public void SetDefaultProvider(string? providerName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var providersOptions = _providersOptionsMonitor.CurrentValue;
        providersOptions.CurrentDefaultProvider = string.IsNullOrWhiteSpace(providerName) ? null : providerName.Trim();
        _normalizer.NormalizeProviders(providersOptions);
        _userProvidersFileService.Save(providersOptions);
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
            Enabled = server.Enabled,
            Name = server.Name,
            Url = server.Url,
            Headers = new Dictionary<string, string>(server.Headers, StringComparer.OrdinalIgnoreCase),
        };

    private static ImportedSkillOptions CloneSkill(ImportedSkillOptions skill)
        => new()
        {
            Enabled = skill.Enabled,
            Name = skill.Name,
            Path = skill.Path,
            Description = skill.Description,
            ImportedAt = skill.ImportedAt,
        };
}
