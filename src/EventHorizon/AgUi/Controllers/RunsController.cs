using System.Text.Json;
using System.Text.Json.Serialization;
using EventHorizon.AGUI.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.AGUI.Controllers;

[ApiController]
[Route("api/runs")]
public sealed class RunsController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly RunService _runService;

    public RunsController(RunService runService)
    {
        _runService = runService;
    }

    [HttpPost]
    public async Task<ActionResult<AGUIRunDTO>> CreateAsync(CreateAGUIRunRequestDTO request, CancellationToken cancellationToken)
    {
        try
        {
            var run = await _runService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return Created($"/api/runs/{run.Id}", run);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [ex.Message],
            }));
        }
    }

    [HttpGet("{runId}")]
    public ActionResult<AGUIRunDTO> GetAsync(string runId)
    {
        var run = _runService.Get(runId);
        return run is null ? NotFound() : Ok(run);
    }

    [HttpPost("{runId}/cancel")]
    public IActionResult CancelAsync(string runId)
    {
        var run = _runService.Get(runId);
        if (run is null)
        {
            return NotFound();
        }

        _runService.Cancel(runId);
        return AcceptedAtAction(nameof(GetAsync), new { runId }, run);
    }

    [HttpGet("{runId}/changes")]
    public async Task<IActionResult> GetChangesAsync(string runId, CancellationToken cancellationToken)
    {
        var changes = await _runService.GetChangesAsync(runId, cancellationToken).ConfigureAwait(false);
        return changes is null ? NotFound() : Ok(changes);
    }

    [HttpGet("{runId}/diff")]
    public async Task<IActionResult> GetDiffAsync(string runId, [FromQuery] string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["path"] = ["Query string parameter 'path' is required."],
            }));
        }

        var run = _runService.Get(runId);
        if (run is null)
        {
            return NotFound();
        }

        var diff = await _runService.GetDiffAsync(runId, path, cancellationToken).ConfigureAwait(false);
        return diff is null ? NotFound() : Ok(diff);
    }

    [HttpGet("{runId}/events")]
    public async Task GetEventsAsync(string runId, CancellationToken cancellationToken)
    {
        var lastEventId = ReadLastEventId(Request);
        var stream = _runService.StreamEventsAsync(runId, lastEventId, cancellationToken);
        if (stream is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.ContentType = "text/event-stream";

        await foreach (var @event in stream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payload = JsonSerializer.Serialize(@event, JsonSerializerOptions);
            await Response.WriteAsync($"id: {@event.Sequence}\n", cancellationToken).ConfigureAwait(false);
            await Response.WriteAsync($"event: {@event.Type}\n", cancellationToken).ConfigureAwait(false);
            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static long? ReadLastEventId(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Last-Event-ID", out var values))
        {
            return null;
        }

        return long.TryParse(values.ToString(), out var value) ? value : null;
    }
}
