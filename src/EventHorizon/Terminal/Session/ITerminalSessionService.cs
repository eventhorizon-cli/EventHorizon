using EventHorizon.Terminal;
using Microsoft.Agents.AI;

namespace EventHorizon.Terminal.Session;

public interface ITerminalSessionService
{
    AgentSession? CurrentSession { get; }

    Task<AgentSession> EnsureSessionAsync(CancellationToken cancellationToken);

    Task<AgentSession> ResetAsync(CancellationToken cancellationToken);

    Task SaveAsync(string sessionName, TerminalConversationState state, CancellationToken cancellationToken);

    Task<TerminalSessionRestoreResult> RestoreAsync(string sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Conversations.ConversationSessionSummary>> ListAsync(CancellationToken cancellationToken);
}

