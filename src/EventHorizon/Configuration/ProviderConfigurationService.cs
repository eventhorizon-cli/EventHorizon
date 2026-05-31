using Microsoft.Extensions.Options;

namespace EventHorizon.Configuration;

public sealed record ConfiguredProvider(string Name, string Type, string? Model);

public interface IProviderConfigurationService
{
    IReadOnlyList<ConfiguredProvider> GetConfiguredProviders();

    string? GetEffectiveProviderName();

    void SetCurrentProvider(string providerName, bool persist);

    void EnsureCurrentProvider(CancellationToken cancellationToken);

    ProviderOptions GetActiveProvider();
}

internal sealed class ProviderConfigurationService : IProviderConfigurationService
{
    private readonly IOptionsMonitor<ProvidersOptions> _optionsMonitor;
    private readonly IOptionsNormalizer _normalizer;
    private readonly IUserProvidersFileService _userProvidersFileService;

    public ProviderConfigurationService(
        IOptionsMonitor<ProvidersOptions> optionsMonitor,
        IOptionsNormalizer normalizer,
        IUserProvidersFileService userProvidersFileService)
    {
        _optionsMonitor = optionsMonitor;
        _normalizer = normalizer;
        _userProvidersFileService = userProvidersFileService;
    }

    private ProvidersOptions Options => _optionsMonitor.CurrentValue;

    public IReadOnlyList<ConfiguredProvider> GetConfiguredProviders()
        => Options.Providers
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static pair => new ConfiguredProvider(
                pair.Key,
                pair.Value.Type ?? "openai",
                pair.Value.Deployment ?? pair.Value.Model))
            .ToArray();

    public string? GetEffectiveProviderName()
    {
        var options = Options;
        if (!string.IsNullOrWhiteSpace(options.CurrentDefaultProvider))
        {
            return options.CurrentDefaultProvider;
        }

        return options.Providers.Count == 1 ? options.Providers.Keys.Single() : null;
    }

    public ProviderOptions GetActiveProvider()
        => _normalizer.ResolveActiveProvider(Options);

    public void SetCurrentProvider(string providerName, bool persist)
    {
        var options = Options;
        if (!options.Providers.ContainsKey(providerName))
        {
            var available = string.Join(", ", options.Providers.Keys.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase));
            throw new InvalidOperationException($"Unknown provider '{providerName}'. Available providers: {available}.");
        }

        options.CurrentDefaultProvider = providerName;
        _normalizer.NormalizeProviders(options);

        if (persist)
        {
            _userProvidersFileService.Save(options);
        }
    }

    public void EnsureCurrentProvider(CancellationToken cancellationToken)
    {
        var options = Options;
        _normalizer.NormalizeProviders(options);

        if (options.Providers.Count == 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.CurrentDefaultProvider))
        {
            return;
        }

        var providers = GetConfiguredProviders();
        if (providers.Count == 1)
        {
            SetCurrentProvider(providers[0].Name, persist: true);
            return;
        }

        if (providers.Count > 1)
        {
            var selection = PromptForProvider(providers, cancellationToken);
            SetCurrentProvider(selection, persist: true);
        }

    }

    private static string PromptForProvider(IReadOnlyList<ConfiguredProvider> providers, CancellationToken cancellationToken)
    {
        Console.WriteLine("Multiple providers are configured, but CurrentDefaultProvider is not set.");
        Console.WriteLine("Choose the provider to use and persist to ~/.eventhorizon/providers.json:");

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
