using EventHorizon.Conversations;
using EventHorizon.Providers;

namespace EventHorizon.AGUI;

internal sealed class ConversationModelService : IConversationModelService
{
    private readonly IConversationSessionStore _conversationSessionStore;
    private readonly IProviderResolutionService _providerResolutionService;
    private readonly IConversationAgentManager _conversationAgentManager;

    public ConversationModelService(
        IConversationSessionStore conversationSessionStore,
        IProviderResolutionService providerResolutionService,
        IConversationAgentManager conversationAgentManager)
    {
        _conversationSessionStore = conversationSessionStore;
        _providerResolutionService = providerResolutionService;
        _conversationAgentManager = conversationAgentManager;
    }

    public async Task<ConversationModelUpdateResult?> UpdateAsync(
        string conversationId,
        string? providerName,
        string? modelId,
        CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(conversationId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        var normalizedProviderName = NormalizeProviderName(providerName);
        var normalizedModelId = NormalizeModelId(modelId);

        var resolved = _providerResolutionService.TryResolveForSession(
            document,
            new ChatRequestOverrides
            {
                ProviderName = normalizedProviderName,
                Model = normalizedModelId,
            });

        if (resolved is null)
        {
            throw new ConversationModelUpdateException(BuildResolutionError(normalizedProviderName, normalizedModelId));
        }

        if (!string.IsNullOrWhiteSpace(normalizedProviderName) &&
            !string.Equals(resolved.ProviderName, normalizedProviderName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConversationModelUpdateException($"Provider '{normalizedProviderName}' is not available.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedModelId) &&
            !string.Equals(resolved.Model, normalizedModelId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConversationModelUpdateException($"Model '{normalizedModelId}' is not available for provider '{resolved.ProviderName ?? "default"}'.");
        }

        document.ProviderName = normalizedProviderName;
        document.ProviderType = resolved.ProviderType;
        document.Model = resolved.Model;
        document.UpdatedAt = DateTimeOffset.UtcNow;

        await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        await _conversationAgentManager.RebuildAsync(document, ChatRequestOverrides.Empty, cancellationToken).ConfigureAwait(false);

        return new ConversationModelUpdateResult
        {
            ConversationId = document.Id,
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
