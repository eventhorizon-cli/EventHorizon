namespace EventHorizon.Configuration;

internal sealed class AppConfigurationService : IAppConfigurationService
{
    private readonly AppOptions _options;
    private readonly IAppOptionsInitializer _initializer;
    private readonly IUserConfigurationFileService _userConfigurationFileService;

    public AppConfigurationService(
        AppOptions options,
        IAppOptionsInitializer initializer,
        IUserConfigurationFileService userConfigurationFileService)
    {
        _options = options;
        _initializer = initializer;
        _userConfigurationFileService = userConfigurationFileService;
    }

    public AppOptions Get()
        => _options;

    public Task<AppOptions> SaveAsync(AppOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CopyInto(_options, options);
        _initializer.Initialize(_options);
        _userConfigurationFileService.Save(_options);
        return Task.FromResult(_options);
    }

    public Task<AppOptions> SetDefaultProviderAsync(string? providerName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _options.CurrentDefaultProvider = string.IsNullOrWhiteSpace(providerName) ? null : providerName.Trim();
        _initializer.Initialize(_options);
        _userConfigurationFileService.Save(_options);
        return Task.FromResult(_options);
    }

    private static void CopyInto(AppOptions target, AppOptions source)
    {
        target.AgUi = source.AgUi;
        target.Agent = source.Agent;
        target.Provider = source.Provider;
        target.CurrentDefaultProvider = source.CurrentDefaultProvider;
        target.Providers = new Dictionary<string, ProviderOptions>(source.Providers, StringComparer.OrdinalIgnoreCase);
        target.Pricing = source.Pricing;
        target.Conversation = source.Conversation;
        target.McpServers = [.. source.McpServers];
        target.Skills = source.Skills;
        target.CurrentProvider = null;
    }
}

