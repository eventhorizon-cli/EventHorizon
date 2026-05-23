using EventHorizon.Configuration;
using EventHorizon.Conversations;
using Microsoft.Agents.AI;

namespace EventHorizon.Terminal.Session;

public sealed class TerminalSessionService : ITerminalSessionService
{
    private readonly AIAgent _agent;
    private readonly AppOptions _options;
    private readonly IConversationSessionStore _sessionStore;
    private readonly IConversationSessionSerializer _sessionSerializer;
    private readonly IConversationSessionMapper _sessionMapper;
    private AgentSession? _currentSession;

    public TerminalSessionService(
        AIAgent agent,
        AppOptions options,
        IConversationSessionStore sessionStore,
        IConversationSessionSerializer sessionSerializer,
        IConversationSessionMapper sessionMapper)
    {
        _agent = agent;
        _options = options;
        _sessionStore = sessionStore;
        _sessionSerializer = sessionSerializer;
        _sessionMapper = sessionMapper;
    }

    public AgentSession? CurrentSession => _currentSession;

    public async Task<AgentSession> EnsureSessionAsync(CancellationToken cancellationToken)
    {
        _currentSession ??= await _agent.CreateSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return _currentSession;
    }

    public async Task<AgentSession> ResetAsync(CancellationToken cancellationToken)
    {
        _currentSession = await _agent.CreateSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return _currentSession;
    }

    public async Task SaveAsync(string sessionName, TerminalConversationState state, CancellationToken cancellationToken)
    {
        string? serializedSession = _currentSession is null ? null : _sessionSerializer.Serialize(_currentSession);
        ConversationSessionDocument document = _sessionMapper.MapToDocument(sessionName, _options, state, serializedSession);
        await _sessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TerminalSessionRestoreResult> RestoreAsync(string sessionId, CancellationToken cancellationToken)
    {
        ConversationSessionDocument? document = await _sessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            throw new InvalidOperationException($"The session '{sessionId}' was not found.");
        }

        bool requiresTranscriptReplay;
        AgentSession? restoredSession = document.SerializedSession is null
            ? null
            : _sessionSerializer.Deserialize(document.SerializedSession);

        if (restoredSession is not null)
        {
            _currentSession = restoredSession;
            requiresTranscriptReplay = false;
        }
        else if (_agent is ChatClientAgent chatClientAgent && !string.IsNullOrWhiteSpace(document.ConversationId))
        {
            _currentSession = await chatClientAgent.CreateSessionAsync(document.ConversationId, cancellationToken).ConfigureAwait(false);
            requiresTranscriptReplay = false;
        }
        else
        {
            _currentSession = await _agent.CreateSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            requiresTranscriptReplay = document.Transcript.Count > 0;
        }

        return new TerminalSessionRestoreResult(_sessionMapper.MapToState(document), requiresTranscriptReplay);
    }

    public Task<IReadOnlyList<ConversationSessionSummary>> ListAsync(CancellationToken cancellationToken)
        => _sessionStore.ListAsync(cancellationToken);
}

