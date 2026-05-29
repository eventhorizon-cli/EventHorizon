namespace EventHorizon.Configuration;

public sealed record ConfiguredProvider(string Name, string Type, string? Model);

public interface IProviderConfigurationService
{
    IReadOnlyList<ConfiguredProvider> GetConfiguredProviders();

    string? GetEffectiveProviderName();

    void SetCurrentProvider(string providerName, bool persist);

    Task EnsureCurrentProviderAsync(CancellationToken cancellationToken);
}

internal sealed class ProviderConfigurationService : IProviderConfigurationService
{
    private readonly AppOptions _options;
    private readonly EffectiveCommandOptions _commandOptions;
    private readonly IAppOptionsInitializer _initializer;
    private readonly IUserConfigurationFileService _userConfigurationFileService;

    public ProviderConfigurationService(
        AppOptions options,
        EffectiveCommandOptions commandOptions,
        IAppOptionsInitializer initializer,
        IUserConfigurationFileService userConfigurationFileService)
    {
        _options = options;
        _commandOptions = commandOptions;
        _initializer = initializer;
        _userConfigurationFileService = userConfigurationFileService;
    }

    public IReadOnlyList<ConfiguredProvider> GetConfiguredProviders()
        => _options.Providers
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static pair => new ConfiguredProvider(
                pair.Key,
                pair.Value.Type ?? "openai",
                pair.Value.Deployment ?? pair.Value.Model))
            .ToArray();

    public string? GetEffectiveProviderName()
    {
        if (!string.IsNullOrWhiteSpace(_commandOptions.Provider) && _options.Providers.ContainsKey(_commandOptions.Provider))
        {
            return _commandOptions.Provider;
        }

        if (!string.IsNullOrWhiteSpace(_options.CurrentDefaultProvider))
        {
            return _options.CurrentDefaultProvider;
        }

        return _options.Providers.Count == 1 ? _options.Providers.Keys.Single() : null;
    }

    public void SetCurrentProvider(string providerName, bool persist)
    {
        if (!_options.Providers.ContainsKey(providerName))
        {
            var available = string.Join(", ", _options.Providers.Keys.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase));
            throw new InvalidOperationException($"Unknown provider '{providerName}'. Available providers: {available}.");
        }

        _options.CurrentDefaultProvider = providerName;
        _initializer.RefreshActiveProvider(_options);

        if (persist)
        {
            _userConfigurationFileService.Save(_options);
        }
    }

    public Task EnsureCurrentProviderAsync(CancellationToken cancellationToken)
    {
        _initializer.RefreshActiveProvider(_options);

        if (!string.IsNullOrWhiteSpace(_commandOptions.Provider))
        {
            return Task.CompletedTask;
        }

        if (_options.Providers.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (!string.IsNullOrWhiteSpace(_options.CurrentDefaultProvider))
        {
            return Task.CompletedTask;
        }

        var providers = GetConfiguredProviders();
        if (providers.Count == 1)
        {
            SetCurrentProvider(providers[0].Name, persist: true);
            return Task.CompletedTask;
        }

        if (providers.Count > 1)
        {
            var selection = PromptForProvider(providers, cancellationToken);
            SetCurrentProvider(selection, persist: true);
        }

        return Task.CompletedTask;
    }

    private static string PromptForProvider(IReadOnlyList<ConfiguredProvider> providers, CancellationToken cancellationToken)
    {
        Console.WriteLine("Multiple providers are configured, but CurrentDefaultProvider is not set.");
        Console.WriteLine("Choose the provider to use and persist to ~/.eventhorizon/appsettings.json:");

        for (var index = 0; index < providers.Count; index++)
        {
            var provider = providers[index];
            var model = string.IsNullOrWhiteSpace(provider.Model) ? "-" : provider.Model;
            Console.WriteLine($"  {index + 1}. {provider.Name} ({provider.Type}, model: {model})");
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("provider> ");
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (int.TryParse(input, out var selectionIndex) && selectionIndex >= 1 && selectionIndex <= providers.Count)
            {
                return providers[selectionIndex - 1].Name;
            }

            var provider = providers.FirstOrDefault(item => string.Equals(item.Name, input, StringComparison.OrdinalIgnoreCase));
            if (provider is not null)
            {
                return provider.Name;
            }

            Console.WriteLine("Please enter a provider number or name from the list above.");
        }

        throw new OperationCanceledException("Provider selection was canceled.", cancellationToken);
    }
}

