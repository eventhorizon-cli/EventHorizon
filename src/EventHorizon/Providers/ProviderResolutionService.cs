using EventHorizon.Configuration;
using EventHorizon.Engine.Sessions;
using Microsoft.Extensions.Options;

namespace EventHorizon.Providers;

internal sealed class ProviderResolutionService : IProviderResolutionService
{
    private readonly IOptionsMonitor<ProvidersOptions> _optionsMonitor;

    public ProviderResolutionService(IOptionsMonitor<ProvidersOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    private ProvidersOptions Options => _optionsMonitor.CurrentValue;

    public IReadOnlyList<ProviderOptions> GetProviderOptions()
        => Options.Providers
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static pair => Clone(pair.Key, pair.Value))
            .ToArray();

    public ResolvedProviderContext? TryResolveForSession(SessionDocument session)
    {
        var options = Options;
        var providerName = FirstNonEmpty(session.ProviderName, options.CurrentDefaultProvider);
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return TryResolveDefaultWithModel(session.Model);
        }

        if (!options.Providers.TryGetValue(providerName, out var provider))
        {
            return null;
        }

        var cloned = Clone(providerName, provider);
        ApplyModelOverride(cloned, session.Model);
        return new ResolvedProviderContext(providerName, cloned.Type ?? "openai", cloned.Model ?? string.Empty, cloned);
    }


    private ResolvedProviderContext? TryResolveDefaultWithModel(string? model)
    {
        var options = Options;

        if (!string.IsNullOrWhiteSpace(options.CurrentDefaultProvider) &&
            options.Providers.TryGetValue(options.CurrentDefaultProvider, out var defaultProvider) &&
            HasConfiguredProvider(defaultProvider))
        {
            var provider = Clone(options.CurrentDefaultProvider, defaultProvider);
            ApplyModelOverride(provider, model);
            return new ResolvedProviderContext(options.CurrentDefaultProvider, provider.Type ?? "openai", provider.Model ?? string.Empty, provider);
        }

        if (options.Providers.Count == 1)
        {
            var pair = options.Providers.Single();
            var provider = Clone(pair.Key, pair.Value);
            ApplyModelOverride(provider, model);
            return new ResolvedProviderContext(pair.Key, provider.Type ?? "openai", provider.Model ?? string.Empty, provider);
        }

        return null;
    }

    private static void ApplyModelOverride(ProviderOptions provider, string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return;
        }

        provider.Model = model.Trim();
        if (string.Equals(provider.Type, "azure-openai", StringComparison.OrdinalIgnoreCase))
        {
            provider.Deployment = provider.Model;
        }

        if (!provider.Models.Contains(provider.Model, StringComparer.OrdinalIgnoreCase))
        {
            provider.Models.Insert(0, provider.Model);
        }
    }

    private static ProviderOptions Clone(string? name, ProviderOptions provider)
        => new()
        {
            Name = name,
            Type = provider.Type,
            Model = provider.Model,
            Models = [.. provider.Models],
            ApiKey = provider.ApiKey,
            Endpoint = provider.Endpoint,
            Deployment = provider.Deployment,
            UseDefaultAzureCredential = provider.UseDefaultAzureCredential,
        };

    private static bool HasConfiguredProvider(ProviderOptions provider)
        => !string.IsNullOrWhiteSpace(provider.Type)
           || !string.IsNullOrWhiteSpace(provider.Model)
           || !string.IsNullOrWhiteSpace(provider.ApiKey)
           || !string.IsNullOrWhiteSpace(provider.Endpoint)
           || !string.IsNullOrWhiteSpace(provider.Deployment);

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
}
