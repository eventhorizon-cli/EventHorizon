using EventHorizon.DTOs;

namespace EventHorizon.Engine.Sessions;

public interface ISessionService
{
    Task<IReadOnlyList<SessionSummaryDTO>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkspaceSummaryDTO>> ListWorkspacesAsync(CancellationToken cancellationToken);

    Task<SessionDetailDTO?> GetAsync(string sessionId, CancellationToken cancellationToken);

    Task<SessionDocument?> GetDocumentAsync(string sessionId, CancellationToken cancellationToken);

    Task<WorkspaceSummaryDTO> CreateWorkspaceAsync(CreateWorkspaceRequestDTO request, CancellationToken cancellationToken);

    Task<SessionSummaryDTO?> CreateWorkspaceSessionAsync(string workspaceId, CreateWorkspaceSessionRequestDTO request, CancellationToken cancellationToken);

    Task<SessionSummaryDTO?> UpdateAsync(string sessionId, UpdateSessionRequestDTO request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken);

    Task<bool> DeleteWorkspaceAsync(string workspaceId, CancellationToken cancellationToken);

    Task<SessionDocument?> StartRunAsync(
        string sessionId,
        string runId,
        string task,
        CancellationToken cancellationToken);

    Task RecordRunCompletedAsync(string sessionId, string? assistantMessage, int changedFilesCount, CancellationToken cancellationToken);

    Task RecordRunFailedAsync(string sessionId, string error, CancellationToken cancellationToken);

    Task RecordRunCancelledAsync(string sessionId, CancellationToken cancellationToken);

    Task GenerateTitleIfNeededAsync(string sessionId, CancellationToken cancellationToken);
}
