namespace EventHorizon.Conversations;

public interface IConversationSessionStore
{
    Task SaveAsync(ConversationSessionDocument document, CancellationToken cancellationToken);

    Task<ConversationSessionDocument?> LoadAsync(string sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConversationSessionSummary>> ListAsync(CancellationToken cancellationToken);

    Task DeleteAsync(string sessionId, CancellationToken cancellationToken);
}

