using System.Text.Json;
using System.Text.Json.Serialization;
using EventHorizon.DTOs;
using EventHorizon.Engine;
using EventHorizon.Engine.Runs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.Controllers;

[ApiController]
[Route("api/sessions/{sessionId}/runs")]
public sealed class RunsController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IRunService _runService;

    public RunsController(IRunService runService)
    {
        _runService = runService;
    }

    [HttpPost]
    public async Task<ActionResult<RunDTO>> CreateAsync(string sessionId, CreateRunRequestDTO request, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request.SessionId) &&
                !string.Equals(request.SessionId, sessionId, StringComparison.Ordinal))
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [nameof(request.SessionId)] = ["SessionId must match the route session id."],
                }));
            }

            request.SessionId = sessionId;
            var run = await _runService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return Created($"/api/sessions/{sessionId}/runs/{run.Id}", run);
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
    public ActionResult<RunDTO> GetAsync(string sessionId, string runId)
    {
        var run = _runService.Get(sessionId, runId);
        return run is null ? NotFound() : Ok(run);
    }

    [HttpPost("{runId}/cancel")]
    public IActionResult CancelAsync(string sessionId, string runId)
    {
        var run = _runService.Get(sessionId, runId);
        if (run is null)
        {
            return NotFound();
        }

        if (!_runService.Cancel(sessionId, runId))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Run cannot be cancelled.",
                Detail = "Only active running runs can be cancelled.",
                Status = StatusCodes.Status409Conflict,
            });
        }

        return AcceptedAtAction(nameof(GetAsync), new { sessionId, runId }, run);
    }

    [HttpGet("{runId}/changes")]
    public IActionResult GetChanges(string sessionId, string runId, CancellationToken cancellationToken)
    {
        var changes = _runService.GetChanges(sessionId, runId, cancellationToken);
        return changes is null ? NotFound() : Ok(changes);
    }

    [HttpGet("{runId}/diff")]
    public IActionResult GetDiff(string sessionId, string runId, [FromQuery] string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["path"] = ["Query string parameter 'path' is required."],
            }));
        }

        var run = _runService.Get(sessionId, runId);
        if (run is null)
        {
            return NotFound();
        }

        var diff = _runService.GetDiff(sessionId, runId, path, cancellationToken);
        return diff is null ? NotFound() : Ok(diff);
    }

    [HttpGet("{runId}/events")]
    public async Task GetEventsAsync(string sessionId, string runId, CancellationToken cancellationToken)
    {
        var lastEventId = ReadLastEventId(Request);
        var stream = _runService.StreamEventsAsync(sessionId, runId, lastEventId, cancellationToken);
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
