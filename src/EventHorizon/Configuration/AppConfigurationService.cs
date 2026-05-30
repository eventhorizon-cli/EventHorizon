using Microsoft.Extensions.Options;

namespace EventHorizon.Configuration;

internal sealed class AppConfigurationService : IAppConfigurationService
{
    private readonly AppOptions _options;
    private readonly IAppOptionsInitializer _initializer;
    private readonly IUserConfigurationFileService _userConfigurationFileService;
    private readonly IUserProvidersFileService _userProvidersFileService;

    public AppConfigurationService(
        IOptions<AppOptions> options,
        IAppOptionsInitializer initializer,
        IUserConfigurationFileService userConfigurationFileService,
        IUserProvidersFileService userProvidersFileService)
    {
        _options = options.Value;
        _initializer = initializer;
        _userConfigurationFileService = userConfigurationFileService;
        _userProvidersFileService = userProvidersFileService;
    }

    public AppOptions Get()
        => _options;

    public Task<AppOptions> SaveAsync(AppOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CopyInto(_options, options);
        _initializer.Initialize(_options);
        _userConfigurationFileService.Save(_options);
        _userProvidersFileService.Save(_options);
        return Task.FromResult(_options);
    }

    public Task<AppOptions> SetDefaultProviderAsync(string? providerName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _options.CurrentDefaultProvider = string.IsNullOrWhiteSpace(providerName) ? null : providerName.Trim();
        _initializer.Initialize(_options);
        _userProvidersFileService.Save(_options);
        return Task.FromResult(_options);
    }

    private static void CopyInto(AppOptions target, AppOptions source)
    {
        target.AGUI = source.AGUI;
        target.Agent = source.Agent;
        target.Provider = source.Provider;
        target.CurrentDefaultProvider = source.CurrentDefaultProvider;
        target.Providers = source.Providers.ToDictionary(
            static pair => pair.Key,
            pair => MergeProvider(pair.Value, target.Providers.TryGetValue(pair.Key, out var existingProvider) ? existingProvider : null),
            StringComparer.OrdinalIgnoreCase);
        target.Pricing = source.Pricing;
        target.Conversation = source.Conversation;
        target.McpServers = [.. source.McpServers];
        target.Skills = source.Skills;
        target.CurrentProvider = null;
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
}
