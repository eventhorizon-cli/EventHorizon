namespace EventHorizon.Engine.Sessions;

public interface ISessionStore
{
    Task SaveAsync(SessionDocument document, CancellationToken cancellationToken);

    Task<SessionDocument?> LoadAsync(string sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SessionSummary>> ListAsync(CancellationToken cancellationToken);

    void Delete(string sessionId, CancellationToken cancellationToken);
}
