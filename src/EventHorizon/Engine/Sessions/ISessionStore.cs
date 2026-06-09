namespace EventHorizon.Engine.Sessions;

public interface ISessionStore
{
    Task SaveWorkspaceAsync(WorkspaceDocument workspace, CancellationToken cancellationToken);

    Task<WorkspaceDocument?> LoadWorkspaceAsync(string workspaceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkspaceDocument>> ListWorkspacesAsync(CancellationToken cancellationToken);

    void DeleteWorkspace(string workspaceId, CancellationToken cancellationToken);

    Task SaveAsync(SessionDocument document, CancellationToken cancellationToken);

    Task<SessionDocument?> LoadAsync(string sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SessionSummary>> ListAsync(CancellationToken cancellationToken);

    void Delete(string sessionId, CancellationToken cancellationToken);
}
