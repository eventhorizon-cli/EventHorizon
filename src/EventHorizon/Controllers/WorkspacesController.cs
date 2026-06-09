using EventHorizon.DTOs;
using EventHorizon.Engine.Sessions;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.Controllers;

[ApiController]
[Route("api/workspaces")]
public sealed class WorkspacesController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public WorkspacesController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public Task<IReadOnlyList<WorkspaceSummaryDTO>> ListAsync(CancellationToken cancellationToken)
        => _sessionService.ListWorkspacesAsync(cancellationToken);

    [HttpPost]
    public async Task<ActionResult<WorkspaceSummaryDTO>> CreateAsync(CreateWorkspaceRequestDTO request, CancellationToken cancellationToken)
    {
        var workspace = await _sessionService.CreateWorkspaceAsync(request, cancellationToken).ConfigureAwait(false);
        return Created($"/api/workspaces/{workspace.Id}", workspace);
    }

    [HttpPost("{workspaceId}/sessions")]
    public async Task<ActionResult<SessionSummaryDTO>> CreateSessionAsync(
        string workspaceId,
        CreateWorkspaceSessionRequestDTO request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionService.CreateWorkspaceSessionAsync(workspaceId, request, cancellationToken).ConfigureAwait(false);
        return session is null ? NotFound() : Created($"/api/sessions/{session.Id}", session);
    }

    [HttpDelete("{workspaceId}")]
    public async Task<IActionResult> DeleteAsync(string workspaceId, CancellationToken cancellationToken)
    {
        var deleted = await _sessionService.DeleteWorkspaceAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        return deleted ? NoContent() : NotFound();
    }
}
