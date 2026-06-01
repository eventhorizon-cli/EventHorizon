namespace EventHorizon.Configuration;

internal interface IOptionsNormalizer
{
    void NormalizeProviders(ProvidersOptions options);

    void NormalizeMcp(McpOptions options);

    void NormalizeSkills(SkillsOptions options);

    ProviderOptions ResolveActiveProvider(ProvidersOptions options);
}

internal sealed class OptionsNormalizer : IOptionsNormalizer
{
    private readonly IPathEnvironment _pathEnvironment;

    public OptionsNormalizer(IPathEnvironment pathEnvironment)
    {
        _pathEnvironment = pathEnvironment;
    }

    public void NormalizeProviders(ProvidersOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CurrentDefaultProvider) && options.Providers.Count == 1)
        {
            options.CurrentDefaultProvider = options.Providers.Keys.Single();
        }

        foreach (var provider in options.Providers.Values)
        {
            provider.Type = NormalizeProviderType(provider.Type);
            NormalizeProviderModels(provider);
        }
    }

    public void NormalizeMcp(McpOptions options)
    {
        foreach (var server in options.Servers)
        {
            server.Url = string.IsNullOrWhiteSpace(server.Url) ? string.Empty : server.Url.Trim();
            server.Name = string.IsNullOrWhiteSpace(server.Name) ? GetDefaultMcpServerName(server.Url) : server.Name.Trim();
            server.Headers = server.Headers
                .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
                .ToDictionary(
                    static pair => pair.Key.Trim(),
                    static pair => pair.Value.Trim(),
                    StringComparer.OrdinalIgnoreCase);
        }
    }

    public void NormalizeSkills(SkillsOptions options)
    {
        options.StoragePath ??= Path.Combine(_pathEnvironment.HomeDirectory, ".eventhorizon", "skills");
    }

    public void NormalizePricing(PricingOptions options)
    {
        options.CachePath ??= Path.Combine(_pathEnvironment.HomeDirectory, ".eventhorizon", "model_prices_and_context_window.json");
    }

    public ProviderOptions ResolveActiveProvider(ProvidersOptions options)
    {
        ProviderOptions provider;
        if (!string.IsNullOrWhiteSpace(options.CurrentDefaultProvider))
        {
            if (!options.Providers.TryGetValue(options.CurrentDefaultProvider!, out var configuredProvider))
            {
                options.CurrentDefaultProvider = null;
                provider = new ProviderOptions();
            }
            else
            {
                provider = CloneProvider(configuredProvider);
            }
        }
        else if (options.Providers.Count == 1)
        {
            provider = CloneProvider(options.Providers.Values.Single());
        }
        else
        {
            provider = new ProviderOptions();
        }

        provider.Type = NormalizeProviderType(provider.Type);
        return provider;
    }

    private static ProviderOptions CloneProvider(ProviderOptions provider)
        => new()
        {
            Name = provider.Name,
            Type = provider.Type,
            Model = provider.Model,
            Models = [.. provider.Models],
            ApiKey = provider.ApiKey,
            Endpoint = provider.Endpoint,
            Deployment = provider.Deployment,
            UseDefaultAzureCredential = provider.UseDefaultAzureCredential,
        };

    private static void NormalizeProviderModels(ProviderOptions provider)
    {
        provider.Name ??= provider.Type;
        provider.Models = provider.Models
            .Where(static model => !string.IsNullOrWhiteSpace(model))
            .Select(static model => model.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(provider.Model) &&
            !provider.Models.Contains(provider.Model, StringComparer.OrdinalIgnoreCase))
        {
            provider.Models.Insert(0, provider.Model);
        }
    }

    private static string NormalizeProviderType(string? providerType)
        => string.IsNullOrWhiteSpace(providerType)
            ? "openai"
            : providerType.Trim().ToLowerInvariant();

    private static string GetDefaultMcpServerName(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.Host
            : url.Trim();
    }
}
