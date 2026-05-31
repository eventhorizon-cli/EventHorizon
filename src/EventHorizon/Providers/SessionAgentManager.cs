using System.Collections.Concurrent;
using EventHorizon.Configuration;
using EventHorizon.Engine.Sessions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHorizon.Providers;

internal sealed class SessionAgentManager : ISessionAgentManager
{
    private readonly IOptionsMonitor<AgentOptions> _agentOptionsMonitor;
    private readonly IProviderResolutionService _providerResolutionService;
    private readonly IProviderAgentFactory _providerAgentFactory;
    private readonly IEventHorizonRuntime _runtime;
    private readonly ISkillProviderFactory _skillProviderFactory;
    private readonly ISessionSerializer _sessionSerializer;
    private readonly IServiceProvider _services;
    private readonly ILogger<SessionAgentManager> _logger;
    private readonly ConcurrentDictionary<string, CachedSessionAgent> _cache = new(StringComparer.Ordinal);

    public SessionAgentManager(
        IOptionsMonitor<AgentOptions> agentOptionsMonitor,
        IProviderResolutionService providerResolutionService,
        IProviderAgentFactory providerAgentFactory,
        IEventHorizonRuntime runtime,
        ISkillProviderFactory skillProviderFactory,
        ISessionSerializer sessionSerializer,
        IServiceProvider services,
        ILogger<SessionAgentManager> logger)
    {
        _agentOptionsMonitor = agentOptionsMonitor;
        _providerResolutionService = providerResolutionService;
        _providerAgentFactory = providerAgentFactory;
        _runtime = runtime;
        _skillProviderFactory = skillProviderFactory;
        _sessionSerializer = sessionSerializer;
        _services = services;
        _logger = logger;
    }

    public async Task<SessionAgentRuntime> GetOrCreateAsync(
        SessionDocument document,
        ChatRequestOverrides? overrides,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(document.Id, out var cached) && IsReusable(cached, document, overrides))
        {
            return ToRuntime(cached, wasReused: true);
        }

        return await RebuildAsync(document, overrides, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionAgentRuntime> RebuildAsync(
        SessionDocument document,
        ChatRequestOverrides? overrides,
        CancellationToken cancellationToken)
    {
        Invalidate(document.Id, cancellationToken);

        var resolved = _providerResolutionService.TryResolveForSession(document, overrides)
            ?? throw new InvalidOperationException("No provider is configured for the current session.");
        var agentOptions = _agentOptionsMonitor.CurrentValue;
        var skillsProvider = _skillProviderFactory.Create(agentOptions, _services, document);
        var instructions = await _runtime.GetInstructionsAsync(cancellationToken).ConfigureAwait(false);
        var tools = await _runtime.GetToolsAsync(cancellationToken).ConfigureAwait(false);

        var agent = _providerAgentFactory.CreateAgent(
            agentOptions,
            resolved.Provider,
            instructions,
            tools,
            skillsProvider,
            _services);

        var session = RestoreSession(document) ?? await agent.CreateSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var cached = new CachedSessionAgent(document.Id, agent, session, resolved, 0);
        _cache[document.Id] = cached;
        _logger.LogDebug("Created session agent cache entry for session {SessionId} using provider {ProviderName}.", document.Id, resolved.ProviderName ?? "<default>");
        return ToRuntime(cached, wasReused: false);
    }

    public void Invalidate(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _cache.TryRemove(sessionId, out _);
    }

    public void InvalidateAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _cache.Clear();
    }

    public void MarkTranscriptCount(string sessionId, int transcriptCount)
    {
        if (_cache.TryGetValue(sessionId, out var cached))
        {
            _cache[sessionId] = cached with { TranscriptCount = transcriptCount };
        }
    }

    private AgentSession? RestoreSession(SessionDocument document)
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
            _logger.LogWarning(ex, "Failed to restore serialized agent session for session {SessionId}. A new agent session will be created.", document.Id);
            return null;
        }
    }

    private bool IsReusable(CachedSessionAgent cached, SessionDocument document, ChatRequestOverrides? overrides)
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

    private static SessionAgentRuntime ToRuntime(CachedSessionAgent cached, bool wasReused)
        => new()
        {
            SessionId = cached.SessionId,
            Agent = cached.Agent,
            Session = cached.Session,
            ResolvedProvider = cached.ResolvedProvider,
            TranscriptCount = cached.TranscriptCount,
            WasReused = wasReused,
        };

    private sealed record CachedSessionAgent(
        string SessionId,
        AIAgent Agent,
        AgentSession Session,
        ResolvedProviderContext ResolvedProvider,
        int TranscriptCount);
}
