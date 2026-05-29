using System.Collections.Concurrent;
using EventHorizon.Configuration;
using EventHorizon.Conversations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;

namespace EventHorizon.Providers;

internal sealed class ConversationAgentManager : IConversationAgentManager
{
    private readonly AppOptions _options;
    private readonly IProviderResolutionService _providerResolutionService;
    private readonly IProviderAgentFactory _providerAgentFactory;
    private readonly IEventHorizonRuntime _runtime;
    private readonly IConversationSessionSerializer _sessionSerializer;
    private readonly ILogger<ConversationAgentManager> _logger;
    private readonly ConcurrentDictionary<string, CachedConversationAgent> _cache = new(StringComparer.Ordinal);

    public ConversationAgentManager(
        AppOptions options,
        IProviderResolutionService providerResolutionService,
        IProviderAgentFactory providerAgentFactory,
        IEventHorizonRuntime runtime,
        IConversationSessionSerializer sessionSerializer,
        ILogger<ConversationAgentManager> logger)
    {
        _options = options;
        _providerResolutionService = providerResolutionService;
        _providerAgentFactory = providerAgentFactory;
        _runtime = runtime;
        _sessionSerializer = sessionSerializer;
        _logger = logger;
    }

    public Task<ConversationAgentRuntime> GetOrCreateAsync(
        ConversationSessionDocument document,
        ChatRequestOverrides? overrides,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(document.Id, out var cached) && IsReusable(cached, document, overrides))
        {
            return Task.FromResult(ToRuntime(cached, wasReused: true));
        }

        return RebuildAsync(document, overrides, cancellationToken);
    }

    public async Task<ConversationAgentRuntime> RebuildAsync(
        ConversationSessionDocument document,
        ChatRequestOverrides? overrides,
        CancellationToken cancellationToken)
    {
        await InvalidateAsync(document.Id, cancellationToken).ConfigureAwait(false);

        var resolved = _providerResolutionService.TryResolveForSession(document, overrides)
            ?? throw new InvalidOperationException("No provider is configured for the current session.");
        var runtimeOptions = CloneRuntimeOptions(resolved);
        var agent = _providerAgentFactory.CreateAgent(
            runtimeOptions,
            _runtime.Instructions,
            _runtime.Tools,
            _runtime.SkillsProvider,
            _runtime.Services);
        var session = RestoreSession(document) ?? await agent.CreateSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var cached = new CachedConversationAgent(document.Id, agent, session, resolved, 0);
        _cache[document.Id] = cached;
        _logger.LogDebug("Created conversation agent cache entry for session {SessionId} using provider {ProviderName}.", document.Id, resolved.ProviderName ?? "<default>");
        return ToRuntime(cached, wasReused: false);
    }

    public Task InvalidateAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _cache.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    public Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _cache.Clear();
        return Task.CompletedTask;
    }

    public void MarkTranscriptCount(string sessionId, int transcriptCount)
    {
        if (_cache.TryGetValue(sessionId, out var cached))
        {
            _cache[sessionId] = cached with { TranscriptCount = transcriptCount };
        }
    }

    private AgentSession? RestoreSession(ConversationSessionDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.SerializedSession))
        {
            return null;
        }

        try
        {
            return _sessionSerializer.Deserialize(document.SerializedSession);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore serialized agent session for conversation {SessionId}. A new agent session will be created.", document.Id);
            return null;
        }
    }

    private bool IsReusable(CachedConversationAgent cached, ConversationSessionDocument document, ChatRequestOverrides? overrides)
    {
        var requestedProvider = overrides?.ProviderName ?? document.ProviderName;
        var requestedModel = overrides?.Model ?? document.Model;
        if (!string.Equals(cached.ResolvedProvider.ProviderName, requestedProvider, StringComparison.OrdinalIgnoreCase) &&
            !(string.IsNullOrWhiteSpace(cached.ResolvedProvider.ProviderName) && string.IsNullOrWhiteSpace(requestedProvider)))
        {
            return false;
        }

        if (!string.Equals(cached.ResolvedProvider.Model, requestedModel, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(requestedModel))
        {
            return false;
        }

        return true;
    }

    private AppOptions CloneRuntimeOptions(ResolvedProviderContext resolved)
        => new()
        {
            AgUi = _options.AgUi,
            Agent = _options.Agent,
            Provider = resolved.Provider,
            CurrentDefaultProvider = resolved.ProviderName ?? _options.CurrentDefaultProvider,
            Providers = _options.Providers,
            Pricing = _options.Pricing,
            Conversation = _options.Conversation,
            McpServers = _options.McpServers,
            Skills = _options.Skills,
        };

    private static ConversationAgentRuntime ToRuntime(CachedConversationAgent cached, bool wasReused)
        => new()
        {
            SessionId = cached.SessionId,
            Agent = cached.Agent,
            Session = cached.Session,
            ResolvedProvider = cached.ResolvedProvider,
            TranscriptCount = cached.TranscriptCount,
            WasReused = wasReused,
        };

    private sealed record CachedConversationAgent(
        string SessionId,
        AIAgent Agent,
        AgentSession Session,
        ResolvedProviderContext ResolvedProvider,
        int TranscriptCount);
}

