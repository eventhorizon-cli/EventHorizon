using EventHorizon.Conversations;
using Microsoft.Agents.AI;

namespace EventHorizon.Providers;

public interface IConversationAgentManager
{
    Task<ConversationAgentRuntime> GetOrCreateAsync(
        ConversationSessionDocument document,
        ChatRequestOverrides? overrides,
        CancellationToken cancellationToken);

    Task<ConversationAgentRuntime> RebuildAsync(
        ConversationSessionDocument document,
        ChatRequestOverrides? overrides,
        CancellationToken cancellationToken);

    Task InvalidateAsync(string sessionId, CancellationToken cancellationToken = default);

    Task InvalidateAllAsync(CancellationToken cancellationToken = default);

    void MarkTranscriptCount(string sessionId, int transcriptCount);
}

public sealed class ConversationAgentRuntime
{
    public required string SessionId { get; init; }

    public required AIAgent Agent { get; init; }

    public required AgentSession Session { get; init; }

    public required ResolvedProviderContext ResolvedProvider { get; init; }

    public required int TranscriptCount { get; init; }

    public bool WasReused { get; init; }
}

