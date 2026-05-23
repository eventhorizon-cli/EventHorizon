using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EventHorizon.Configuration;

public interface IPathEnvironment
{
    string CurrentDirectory { get; }

    string HomeDirectory { get; }
}

public sealed class PathEnvironment : IPathEnvironment
{
    public string CurrentDirectory => Directory.GetCurrentDirectory();

    public string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
}

internal sealed class AppOptionsPostConfigure : IPostConfigureOptions<AppOptions>
{
    private readonly IConfiguration _configuration;
    private readonly EffectiveCommandOptions _commandOptions;
    private readonly IPathEnvironment _pathEnvironment;

    public AppOptionsPostConfigure(IConfiguration configuration, EffectiveCommandOptions commandOptions, IPathEnvironment pathEnvironment)
    {
        _configuration = configuration;
        _commandOptions = commandOptions;
        _pathEnvironment = pathEnvironment;
    }

    public void PostConfigure(string? name, AppOptions options)
    {
        ApplyEnvironmentFallbacks(options);
        ApplyCommandLineOverrides(options);
        Normalize(options);
    }

    private void ApplyEnvironmentFallbacks(AppOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Provider.Type))
        {
            options.Provider.Type = InferProviderType();
        }

        switch (options.Provider.Type.Trim().ToLowerInvariant())
        {
            case "azure-openai":
                options.Provider.Endpoint ??= _configuration["AZURE_OPENAI_ENDPOINT"];
                options.Provider.ApiKey ??= _configuration["AZURE_OPENAI_API_KEY"];
                options.Provider.Deployment ??= _configuration["AZURE_OPENAI_DEPLOYMENT_NAME"];
                options.Provider.Model ??= options.Provider.Deployment;
                break;
            case "anthropic":
                options.Provider.ApiKey ??= _configuration["ANTHROPIC_API_KEY"];
                options.Provider.Model ??= _configuration["ANTHROPIC_CHAT_MODEL_NAME"] ?? "claude-sonnet-4-20250514";
                break;
            case "gemini":
                options.Provider.ApiKey ??= _configuration["GOOGLE_GENAI_API_KEY"];
                options.Provider.Model ??= _configuration["GOOGLE_GENAI_MODEL"] ?? "gemini-2.5-flash";
                break;
            case "openai-compatible":
                options.Provider.ApiKey ??= _configuration["OPENAI_COMPATIBLE_API_KEY"]
                    ?? _configuration["OPENAI_API_KEY"]
                    ?? _configuration["OLLAMA_API_KEY"];
                options.Provider.Endpoint ??= _configuration["OPENAI_COMPATIBLE_ENDPOINT"]
                    ?? _configuration["OPENAI_BASE_URL"]
                    ?? _configuration["OLLAMA_ENDPOINT"];
                options.Provider.Model ??= _configuration["OPENAI_COMPATIBLE_MODEL"]
                    ?? _configuration["OPENAI_MODEL"]
                    ?? _configuration["OLLAMA_MODEL"]
                    ?? "gpt-4.1-mini";
                break;
            default:
                options.Provider.Type = "openai";
                options.Provider.ApiKey ??= _configuration["OPENAI_API_KEY"];
                options.Provider.Model ??= _configuration["OPENAI_MODEL"]
                    ?? _configuration["OPENAI_CHAT_MODEL_NAME"]
                    ?? "gpt-4.1-mini";
                break;
        }
    }

    private void ApplyCommandLineOverrides(AppOptions options)
    {
        if (!string.IsNullOrWhiteSpace(_commandOptions.WorkspaceRoot))
        {
            options.WorkspaceRoot = _commandOptions.WorkspaceRoot;
        }

        if (!string.IsNullOrWhiteSpace(_commandOptions.ProviderType))
        {
            options.Provider.Type = _commandOptions.ProviderType;
        }

        if (!string.IsNullOrWhiteSpace(_commandOptions.Model))
        {
            options.Provider.Model = _commandOptions.Model;
            if (string.Equals(options.Provider.Type, "azure-openai", StringComparison.OrdinalIgnoreCase))
            {
                options.Provider.Deployment = _commandOptions.Model;
            }
        }

        if (!string.IsNullOrWhiteSpace(_commandOptions.Url))
        {
            if (string.Equals(_commandOptions.Command, "client", StringComparison.OrdinalIgnoreCase))
            {
                options.Protocol.ClientUrl = _commandOptions.Url;
            }
            else
            {
                options.Protocol.Url = _commandOptions.Url;
            }
        }
    }

    private void Normalize(AppOptions options)
    {
        options.WorkspaceRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(options.WorkspaceRoot) ? _pathEnvironment.CurrentDirectory : options.WorkspaceRoot);
        options.Provider.Type = options.Provider.Type.Trim().ToLowerInvariant();
        options.Protocol.Path = string.IsNullOrWhiteSpace(options.Protocol.Path) ? "/agui" : options.Protocol.Path;
        options.Protocol.ClientUrl = string.IsNullOrWhiteSpace(options.Protocol.ClientUrl)
            ? options.Protocol.Url.TrimEnd('/') + options.Protocol.Path
            : options.Protocol.ClientUrl;
        options.Pricing.CachePath ??= Path.Combine(_pathEnvironment.HomeDirectory, ".eventhorizon", "model_prices_and_context_window.json");
        options.Conversation.StoragePath ??= Path.Combine(_pathEnvironment.HomeDirectory, ".eventhorizon", "sessions");

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
}

