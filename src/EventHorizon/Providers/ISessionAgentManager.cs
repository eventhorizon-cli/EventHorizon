using EventHorizon.Engine.Sessions;
using Microsoft.Agents.AI;

namespace EventHorizon.Providers;

public interface ISessionAgentManager
{
    Task<SessionAgentRuntime> GetOrCreateAsync(
        SessionDocument document,
        ChatRequestOverrides? overrides,
        CancellationToken cancellationToken);

    Task<SessionAgentRuntime> RebuildAsync(
        SessionDocument document,
        ChatRequestOverrides? overrides,
        CancellationToken cancellationToken);

    void Invalidate(string sessionId, CancellationToken cancellationToken = default);

    void InvalidateAll(CancellationToken cancellationToken = default);

    void MarkTranscriptCount(string sessionId, int transcriptCount);
}

public sealed class SessionAgentRuntime
{
    public required string SessionId { get; init; }

    public required AIAgent Agent { get; init; }

    public required AgentSession Session { get; init; }

    public required ResolvedProviderContext ResolvedProvider { get; init; }

    public required int TranscriptCount { get; init; }

    public bool WasReused { get; init; }
}
