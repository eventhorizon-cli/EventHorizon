using EventHorizon.Workspace;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.AGUI.Controllers;

[ApiController]
[Route("api/conversations")]
public sealed class ConversationsController : ControllerBase
{
    private readonly IAGUISessionService _sessionService;
    private readonly WorkspaceContext _workspaceContext;

    public ConversationsController(IAGUISessionService sessionService, WorkspaceContext workspaceContext)
    {
        _sessionService = sessionService;
        _workspaceContext = workspaceContext;
    }

    [HttpGet]
    public Task<IReadOnlyList<AGUISessionSummary>> ListAsync(CancellationToken cancellationToken)
        => _sessionService.ListAsync(cancellationToken);

    [HttpGet("{conversationId}")]
    public async Task<ActionResult<AGUISessionDetail>> GetAsync(string conversationId, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetAsync(conversationId, cancellationToken).ConfigureAwait(false);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost]
    public async Task<ActionResult<AGUISessionSummary>> CreateAsync(CreateAGUISessionRequest request, CancellationToken cancellationToken)
    {
        var session = await _sessionService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetAsync), new { conversationId = session.Id }, session);
    }

    [HttpPatch("{conversationId}")]
    public async Task<ActionResult<AGUISessionSummary>> UpdateAsync(string conversationId, UpdateAGUISessionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) && request.ProviderName is null && request.Model is null)
        {
            return ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Title)] = ["At least one update field is required."],
            });
        }

        var session = await _sessionService.UpdateAsync(conversationId, request, cancellationToken).ConfigureAwait(false);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPut("{conversationId}/model")]
    public async Task<ActionResult<ConversationModelResponse>> UpdateModelAsync(
        string conversationId,
        UpdateConversationModelRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionService.UpdateAsync(
            conversationId,
            new UpdateAGUISessionRequest
            {
                ProviderName = request.ProviderName,
                Model = request.ModelId,
            },
            cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return NotFound();
        }

        return Ok(new ConversationModelResponse
        {
            ConversationId = session.Id,
            ProviderName = session.ProviderName,
            ProviderType = session.ProviderType ?? string.Empty,
            ModelId = session.Model ?? string.Empty,
            Warnings = Array.Empty<string>(),
        });
    }

    [HttpDelete("{conversationId}")]
    public async Task<IActionResult> DeleteAsync(string conversationId, CancellationToken cancellationToken)
    {
        var deleted = await _sessionService.DeleteAsync(conversationId, cancellationToken).ConfigureAwait(false);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("directories")]
    public ActionResult<IReadOnlyList<DirectoryItem>> GetDirectories([FromQuery] string? path = null)
    {
        var targetPath = string.IsNullOrWhiteSpace(path)
            ? _workspaceContext.WorkspaceRoot
            : Path.GetFullPath(path);

        if (!Directory.Exists(targetPath))
        {
            return NotFound();
        }

        List<DirectoryItem> items = [];
        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
        {
            var isAtRoot = OperatingSystem.IsWindows()
                ? targetPath.Length <= 3 && targetPath.EndsWith(':')
                : targetPath == "/";
            if (!isAtRoot)
            {
                items.Add(new DirectoryItem(parent, "..", true, parent));
            }
        }

        foreach (var entry in Directory.EnumerateFileSystemEntries(targetPath).OrderBy(static value => value, StringComparer.OrdinalIgnoreCase))
        {
            items.Add(new DirectoryItem(entry, Path.GetFileName(entry), Directory.Exists(entry), targetPath));
        }

        return Ok(items);
    }
}

