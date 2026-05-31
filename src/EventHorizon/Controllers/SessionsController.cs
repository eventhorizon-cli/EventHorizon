using EventHorizon.DTOs;
using EventHorizon.Engine.Sessions;
using EventHorizon.Workspace;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.Controllers;

[ApiController]
[Route("api/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ISessionModelService _sessionModelService;
    private readonly WorkspaceContext _workspaceContext;

    public SessionsController(
        ISessionService sessionService,
        ISessionModelService sessionModelService,
        WorkspaceContext workspaceContext)
    {
        _sessionService = sessionService;
        _sessionModelService = sessionModelService;
        _workspaceContext = workspaceContext;
    }

    [HttpGet]
    public Task<IReadOnlyList<SessionSummaryDTO>> ListAsync(CancellationToken cancellationToken)
        => _sessionService.ListAsync(cancellationToken);

    [HttpGet("{sessionId}")]
    public async Task<ActionResult<SessionDetailDTO>> GetAsync(string sessionId, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost]
    public async Task<ActionResult<SessionSummaryDTO>> CreateAsync(CreateSessionRequestDTO request, CancellationToken cancellationToken)
    {
        var workspaceRoot = ResolveWorkspacePath(request.WorkspaceRoot);
        Directory.CreateDirectory(workspaceRoot);
        request.WorkspaceRoot = workspaceRoot;
        var session = await _sessionService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return Created($"/api/sessions/{session.Id}", session);
    }

    [HttpPatch("{sessionId}")]
    public async Task<ActionResult<SessionSummaryDTO>> UpdateAsync(string sessionId, UpdateSessionRequestDTO request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) && request.ProviderName is null && request.Model is null)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Title)] = ["At least one update field is required."],
            }));
        }

        var session = await _sessionService.UpdateAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPut("{sessionId}/model")]
    public async Task<ActionResult<SessionModelResponseDTO>> UpdateModelAsync(
        string sessionId,
        UpdateSessionModelRequestDTO request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _sessionModelService
                .UpdateAsync(sessionId, request.ProviderName, request.ModelId, cancellationToken)
                .ConfigureAwait(false);
            if (result is null)
            {
                return NotFound();
            }

            return Ok(new SessionModelResponseDTO
            {
                SessionId = result.SessionId,
                ProviderName = result.ProviderName,
                ProviderType = result.ProviderType,
                ModelId = result.ModelId,
                Warnings = result.Warnings,
            });
        }
        catch (SessionModelUpdateException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.ModelId)] = [ex.Message],
            }));
        }
    }

    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteAsync(string sessionId, CancellationToken cancellationToken)
    {
        var deleted = await _sessionService.DeleteAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("directories")]
    public ActionResult<DirectoryListingDTO> GetDirectories([FromQuery] string? path = null)
    {
        var targetPath = ResolveWorkspacePath(path);
        if (!Directory.Exists(targetPath))
        {
            return NotFound();
        }

        List<DirectoryItemDTO> items = [];
        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
        {
            items.Add(new DirectoryItemDTO(parent, "..", true, targetPath));
        }

        foreach (var entry in Directory.EnumerateFileSystemEntries(targetPath).OrderBy(static value => value, StringComparer.OrdinalIgnoreCase))
        {
            items.Add(new DirectoryItemDTO(entry, Path.GetFileName(entry), Directory.Exists(entry), targetPath));
        }

        return Ok(new DirectoryListingDTO(targetPath, items));
    }

    private string ResolveWorkspacePath(string? path)
        => string.IsNullOrWhiteSpace(path)
            ? _workspaceContext.WorkspaceRoot
            : Path.GetFullPath(path);
}
