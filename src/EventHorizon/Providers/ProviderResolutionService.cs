using EventHorizon.Configuration;
using EventHorizon.Conversations;
using Microsoft.Extensions.Options;

namespace EventHorizon.Providers;

internal sealed class ProviderResolutionService : IProviderResolutionService
{
    private readonly AppOptions _options;
    private readonly IProviderConfigurationService _providerConfigurationService;

    public ProviderResolutionService(IOptions<AppOptions> options, IProviderConfigurationService providerConfigurationService)
    {
        _options = options.Value;
        _providerConfigurationService = providerConfigurationService;
    }

    public IReadOnlyList<ProviderOptions> GetProviderOptions()
        => _options.Providers
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static pair => Clone(pair.Key, pair.Value))
            .ToArray();

    public ResolvedProviderContext? TryResolveForSession(ConversationSessionDocument? session, ChatRequestOverrides? overrides = null)
    {
        var providerName = FirstNonEmpty(overrides?.ProviderName, session?.ProviderName, _options.CurrentDefaultProvider, _providerConfigurationService.GetEffectiveProviderName());
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return TryResolveDefaultWithModel(overrides?.Model ?? session?.Model);
        }

        if (!_options.Providers.TryGetValue(providerName, out var provider))
        {
            return null;
        }

        var cloned = Clone(providerName, provider);
        ApplyModelOverride(cloned, overrides?.Model ?? session?.Model);
        return new ResolvedProviderContext(providerName, cloned.Type ?? "openai", cloned.Model ?? string.Empty, cloned, overrides ?? ChatRequestOverrides.Empty);
    }

    public ResolvedProviderContext? TryResolveDefault()
        => TryResolveForSession(session: null, ChatRequestOverrides.Empty);

    private ResolvedProviderContext? TryResolveDefaultWithModel(string? model)
    {
        if (HasConfiguredProvider(_options.Provider))
        {
            var provider = Clone(_options.CurrentDefaultProvider, _options.Provider);
            ApplyModelOverride(provider, model);
            return new ResolvedProviderContext(_options.CurrentDefaultProvider, provider.Type ?? "openai", provider.Model ?? string.Empty, provider, ChatRequestOverrides.Empty);
        }

        if (_options.Providers.Count == 1)
        {
            var pair = _options.Providers.Single();
            var provider = Clone(pair.Key, pair.Value);
            ApplyModelOverride(provider, model);
            return new ResolvedProviderContext(pair.Key, provider.Type ?? "openai", provider.Model ?? string.Empty, provider, ChatRequestOverrides.Empty);
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
