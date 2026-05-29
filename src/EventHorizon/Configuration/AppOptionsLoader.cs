using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EventHorizon.Configuration;

internal sealed class AppOptionsInitializer : IAppOptionsInitializer
{
    private readonly IConfiguration _configuration;
    private readonly EffectiveCommandOptions _commandOptions;
    private readonly IPathEnvironment _pathEnvironment;

    public AppOptionsInitializer(IConfiguration configuration, EffectiveCommandOptions commandOptions, IPathEnvironment pathEnvironment)
    {
        _configuration = configuration;
        _commandOptions = commandOptions;
        _pathEnvironment = pathEnvironment;
    }

    public void Initialize(AppOptions options)
    {
        PromoteLegacyCurrentProvider(options);
        PromoteLegacyProvider(options);
        ApplyEnvironmentFallbacks(options);
        RefreshActiveProvider(options);
        Normalize(options);
    }

    public void RefreshActiveProvider(AppOptions options)
    {
        options.Provider = ResolveActiveProvider(options);
    }

    private void PromoteLegacyProvider(AppOptions options)
    {
        if (HasConfiguredValues(options.Provider) && !options.Providers.ContainsKey("default"))
        {
            options.Providers["default"] = CloneProvider(options.Provider);
        }
    }

    private static void PromoteLegacyCurrentProvider(AppOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CurrentProvider))
        {
            options.CurrentDefaultProvider = options.CurrentProvider;
        }

        if (string.IsNullOrWhiteSpace(options.CurrentDefaultProvider) && options.Providers.Count == 1)
        {
            options.CurrentDefaultProvider = options.Providers.Keys.Single();
        }

        options.CurrentProvider = null;
    }

    private void ApplyEnvironmentFallbacks(AppOptions options)
    {
        foreach (var provider in options.Providers.Values)
        {
            ApplyEnvironmentFallbacks(provider);
        }

        if (options.Providers.Count == 0)
        {
            ApplyEnvironmentFallbacks(options.Provider);
        }
    }

    private void ApplyEnvironmentFallbacks(ProviderOptions provider)
    {
        provider.Type = NormalizeProviderType(string.IsNullOrWhiteSpace(provider.Type) ? InferProviderType() : provider.Type);

        switch (provider.Type)
        {
            case "azure-openai":
                provider.Endpoint ??= _configuration["AZURE_OPENAI_ENDPOINT"];
                provider.ApiKey ??= _configuration["AZURE_OPENAI_API_KEY"];
                provider.Deployment ??= _configuration["AZURE_OPENAI_DEPLOYMENT_NAME"];
                provider.Model ??= provider.Deployment;
                break;
            case "anthropic":
                provider.ApiKey ??= _configuration["ANTHROPIC_API_KEY"];
                provider.Model ??= _configuration["ANTHROPIC_CHAT_MODEL_NAME"] ?? "claude-sonnet-4-20250514";
                break;
            case "gemini":
                provider.ApiKey ??= _configuration["GOOGLE_GENAI_API_KEY"];
                provider.Model ??= _configuration["GOOGLE_GENAI_MODEL"] ?? "gemini-2.5-flash";
                break;
            case "openai-compatible":
                provider.ApiKey ??= _configuration["OPENAI_COMPATIBLE_API_KEY"]
                    ?? _configuration["OPENAI_API_KEY"]
                    ?? _configuration["OLLAMA_API_KEY"];
                provider.Endpoint ??= _configuration["OPENAI_COMPATIBLE_ENDPOINT"]
                    ?? _configuration["OPENAI_BASE_URL"]
                    ?? _configuration["OLLAMA_ENDPOINT"];
                provider.Model ??= _configuration["OPENAI_COMPATIBLE_MODEL"]
                    ?? _configuration["OPENAI_MODEL"]
                    ?? _configuration["OLLAMA_MODEL"]
                    ?? "gpt-4.1-mini";
                break;
            default:
                provider.Type = "openai";
                provider.ApiKey ??= _configuration["OPENAI_API_KEY"];
                provider.Model ??= _configuration["OPENAI_MODEL"]
                    ?? _configuration["OPENAI_CHAT_MODEL_NAME"]
                    ?? "gpt-4.1-mini";
                break;
        }
    }

    private ProviderOptions ResolveActiveProvider(AppOptions options)
    {
        ProviderOptions provider;
        if (!string.IsNullOrWhiteSpace(_commandOptions.Provider) && options.Providers.TryGetValue(_commandOptions.Provider, out var configuredProvider))
        {
            provider = CloneProvider(configuredProvider);
        }
        else if (!string.IsNullOrWhiteSpace(options.CurrentDefaultProvider))
        {
            if (!options.Providers.TryGetValue(options.CurrentDefaultProvider!, out configuredProvider))
            {
                options.CurrentDefaultProvider = null;
                provider = CloneProvider(options.Provider);
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
            provider = CloneProvider(options.Provider);
        }

        ApplyCommandLineOverrides(provider, options);
        provider.Type = NormalizeProviderType(provider.Type);
        return provider;
    }

    private void ApplyCommandLineOverrides(ProviderOptions provider, AppOptions options)
    {
        if (!string.IsNullOrWhiteSpace(_commandOptions.Provider) && !options.Providers.ContainsKey(_commandOptions.Provider))
        {
            provider.Type = _commandOptions.Provider;
        }

        if (!string.IsNullOrWhiteSpace(_commandOptions.Model))
        {
            provider.Model = _commandOptions.Model;
            if (string.Equals(provider.Type, "azure-openai", StringComparison.OrdinalIgnoreCase))
            {
                provider.Deployment = _commandOptions.Model;
            }
        }
    }

    private void Normalize(AppOptions options)
    {
        foreach (var provider in options.Providers.Values)
        {
            provider.Type = NormalizeProviderType(provider.Type);
            NormalizeProviderModels(provider);
        }

        options.Provider.Type = NormalizeProviderType(options.Provider.Type);
        NormalizeProviderModels(options.Provider);
        options.Pricing.CachePath ??= Path.Combine(_pathEnvironment.HomeDirectory, ".eventhorizon", "model_prices_and_context_window.json");
        options.Conversation.StoragePath ??= Path.Combine(_pathEnvironment.HomeDirectory, ".eventhorizon", "sessions");
        options.Skills.StoragePath ??= Path.Combine(_pathEnvironment.HomeDirectory, ".eventhorizon", "skills");

        foreach (var server in options.McpServers)
        {
            server.Name = string.IsNullOrWhiteSpace(server.Name) ? server.Command : server.Name;
        }
    }

    private string InferProviderType()
    {
        if (!string.IsNullOrWhiteSpace(_configuration["AZURE_OPENAI_ENDPOINT"]))
        {
            return "azure-openai";
        }

        if (!string.IsNullOrWhiteSpace(_configuration["ANTHROPIC_API_KEY"]))
        {
            return "anthropic";
        }

        if (!string.IsNullOrWhiteSpace(_configuration["GOOGLE_GENAI_API_KEY"]))
        {
            return "gemini";
        }

        if (!string.IsNullOrWhiteSpace(_configuration["OPENAI_COMPATIBLE_ENDPOINT"]) ||
            !string.IsNullOrWhiteSpace(_configuration["OLLAMA_ENDPOINT"]))
        {
            return "openai-compatible";
        }

        return "openai";
    }

    private static bool HasConfiguredValues(ProviderOptions provider)
        => !string.IsNullOrWhiteSpace(provider.Type)
           || !string.IsNullOrWhiteSpace(provider.Model)
           || !string.IsNullOrWhiteSpace(provider.ApiKey)
           || !string.IsNullOrWhiteSpace(provider.Endpoint)
           || !string.IsNullOrWhiteSpace(provider.Deployment);

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
}

internal sealed class AppOptionsPostConfigure : IPostConfigureOptions<AppOptions>
{
    private readonly IAppOptionsInitializer _initializer;

    public AppOptionsPostConfigure(IAppOptionsInitializer initializer)
    {
        _initializer = initializer;
    }

    public void PostConfigure(string? name, AppOptions options)
        => _initializer.Initialize(options);
}

