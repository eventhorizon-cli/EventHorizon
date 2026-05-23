namespace EventHorizon.Conversations;

public interface IConversationSessionStore
{
    Task SaveAsync(Conversations.ConversationSessionDocument document, CancellationToken cancellationToken);

    Task<Conversations.ConversationSessionDocument?> LoadAsync(string sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Conversations.ConversationSessionSummary>> ListAsync(CancellationToken cancellationToken);
}

