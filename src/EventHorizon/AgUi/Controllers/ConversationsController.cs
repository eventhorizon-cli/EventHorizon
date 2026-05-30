using EventHorizon.AGUI.DTOs;
using EventHorizon.Workspace;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.AGUI.Controllers;

[ApiController]
[Route("api/conversations")]
public sealed class ConversationsController : ControllerBase
{
    private readonly IAGUISessionService _sessionService;
    private readonly IConversationModelService _conversationModelService;
    private readonly WorkspaceContext _workspaceContext;

    public ConversationsController(
        IAGUISessionService sessionService,
        IConversationModelService conversationModelService,
        WorkspaceContext workspaceContext)
    {
        _sessionService = sessionService;
        _conversationModelService = conversationModelService;
        _workspaceContext = workspaceContext;
    }

    [HttpGet]
    public Task<IReadOnlyList<AGUISessionSummaryDTO>> ListAsync(CancellationToken cancellationToken)
        => _sessionService.ListAsync(cancellationToken);

    [HttpGet("{conversationId}")]
    public async Task<ActionResult<AGUISessionDetailDTO>> GetAsync(string conversationId, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetAsync(conversationId, cancellationToken).ConfigureAwait(false);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost]
    public async Task<ActionResult<AGUISessionSummaryDTO>> CreateAsync(CreateAGUISessionRequestDTO request, CancellationToken cancellationToken)
    {
        var workspaceRoot = ResolveWorkspacePath(request.WorkspaceRoot);
        Directory.CreateDirectory(workspaceRoot);
        request.WorkspaceRoot = workspaceRoot;
        var session = await _sessionService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return Created($"/api/conversations/{session.Id}", session);
    }

    [HttpPatch("{conversationId}")]
    public async Task<ActionResult<AGUISessionSummaryDTO>> UpdateAsync(string conversationId, UpdateAGUISessionRequestDTO request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) && request.ProviderName is null && request.Model is null)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Title)] = ["At least one update field is required."],
            }));
        }

        var session = await _sessionService.UpdateAsync(conversationId, request, cancellationToken).ConfigureAwait(false);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPut("{conversationId}/model")]
    public async Task<ActionResult<ConversationModelResponseDTO>> UpdateModelAsync(
        string conversationId,
        UpdateConversationModelRequestDTO request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _conversationModelService
                .UpdateAsync(conversationId, request.ProviderName, request.ModelId, cancellationToken)
                .ConfigureAwait(false);
            if (result is null)
            {
                return NotFound();
            }

            return Ok(new ConversationModelResponseDTO
            {
                ConversationId = result.ConversationId,
                ProviderName = result.ProviderName,
                ProviderType = result.ProviderType,
                ModelId = result.ModelId,
                Warnings = result.Warnings,
            });
        }
        catch (ConversationModelUpdateException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.ModelId)] = [ex.Message],
            }));
        }
    }

    [HttpDelete("{conversationId}")]
    public async Task<IActionResult> DeleteAsync(string conversationId, CancellationToken cancellationToken)
    {
        var deleted = await _sessionService.DeleteAsync(conversationId, cancellationToken).ConfigureAwait(false);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("directories")]
    public ActionResult<IReadOnlyList<DirectoryItemDTO>> GetDirectories([FromQuery] string? path = null)
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
            items.Add(new DirectoryItemDTO(parent, "..", true, parent));
        }

        foreach (var entry in Directory.EnumerateFileSystemEntries(targetPath).OrderBy(static value => value, StringComparer.OrdinalIgnoreCase))
        {
            items.Add(new DirectoryItemDTO(entry, Path.GetFileName(entry), Directory.Exists(entry), targetPath));
        }

        return Ok(items);
    }

    private string ResolveWorkspacePath(string? path)
        => string.IsNullOrWhiteSpace(path)
            ? _workspaceContext.WorkspaceRoot
            : Path.GetFullPath(path);
}
