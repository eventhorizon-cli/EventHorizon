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

    [HttpDelete("{workspaceId}")]
    public async Task<IActionResult> DeleteAsync(string workspaceId, CancellationToken cancellationToken)
    {
        var deleted = await _sessionService.DeleteWorkspaceAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        return deleted ? NoContent() : NotFound();
    }
}
