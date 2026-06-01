using EventHorizon.Engine.Sessions;
using EventHorizon.Providers;

namespace EventHorizon.Engine.Sessions;

internal sealed class SessionModelService : ISessionModelService
{
    private readonly ISessionStore _sessionStore;
    private readonly IProviderResolutionService _providerResolutionService;
    private readonly ISessionAgentManager _agentManager;

    public SessionModelService(
        ISessionStore sessionStore,
        IProviderResolutionService providerResolutionService,
        ISessionAgentManager agentManager)
    {
        _sessionStore = sessionStore;
        _providerResolutionService = providerResolutionService;
        _agentManager = agentManager;
    }

    public async Task<SessionModelUpdateResult?> UpdateAsync(
        string sessionId,
        string? providerName,
        string? modelId,
        CancellationToken cancellationToken)
    {
        var document = await _sessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        var normalizedProviderName = NormalizeProviderName(providerName);
        var normalizedModelId = NormalizeModelId(modelId);

        var resolutionDocument = new SessionDocument
        {
            ProviderName = normalizedProviderName ?? document.ProviderName,
            Model = normalizedModelId ?? document.Model,
        };
        var resolved = _providerResolutionService.TryResolveForSession(resolutionDocument);

        if (resolved is null)
        {
            throw new SessionModelUpdateException(BuildResolutionError(normalizedProviderName, normalizedModelId));
        }

        if (!string.IsNullOrWhiteSpace(normalizedProviderName) &&
            !string.Equals(resolved.ProviderName, normalizedProviderName, StringComparison.OrdinalIgnoreCase))
        {
            throw new SessionModelUpdateException($"Provider '{normalizedProviderName}' is not available.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedModelId) &&
            !string.Equals(resolved.Model, normalizedModelId, StringComparison.OrdinalIgnoreCase))
        {
            throw new SessionModelUpdateException($"Model '{normalizedModelId}' is not available for provider '{resolved.ProviderName ?? "default"}'.");
        }

        document.ProviderName = normalizedProviderName;
        document.ProviderType = resolved.ProviderType;
        document.Model = resolved.Model;
        document.UpdatedAt = DateTimeOffset.UtcNow;

        await _sessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        await _agentManager.RebuildAsync(document, cancellationToken).ConfigureAwait(false);

        return new SessionModelUpdateResult
        {
            SessionId = document.Id,
            ProviderName = document.ProviderName,
            ProviderType = document.ProviderType,
            ModelId = document.Model,
            Warnings = Array.Empty<string>(),
        };
    }

    private static string? NormalizeProviderName(string? providerName)
        => providerName is null ? null : string.IsNullOrWhiteSpace(providerName) ? null : providerName.Trim();

    private static string? NormalizeModelId(string? modelId)
        => modelId is null ? null : string.IsNullOrWhiteSpace(modelId) ? string.Empty : modelId.Trim();

    private static string BuildResolutionError(string? providerName, string? modelId)
    {
        if (!string.IsNullOrWhiteSpace(providerName) && !string.IsNullOrWhiteSpace(modelId))
        {
            return $"Provider '{providerName}' or model '{modelId}' is not available.";
        }

        if (!string.IsNullOrWhiteSpace(providerName))
        {
            return $"Provider '{providerName}' is not available.";
        }

        if (!string.IsNullOrWhiteSpace(modelId))
        {
            return $"Model '{modelId}' is not available.";
        }

        return "No provider is configured. Please open settings and configure a provider before sending messages.";
    }
}
