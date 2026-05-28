using System.Text.Json;
using System.Text.Json.Serialization;
using EventHorizon.Configuration;
using EventHorizon.Providers;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace EventHorizon.AGUI;

public static class AGUIEndpoints
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void Map(WebApplication app, AGUIOptions options, RunService runService, AGUISessionService sessionService, IEventHorizonRuntime runtime)
    {
        var apiBasePath = NormalizePath(options.ApiBasePath);
        var rawEndpointPath = NormalizePath(options.RawEndpointPath);
        MapApi(app, apiBasePath, runService, sessionService);
        app.MapAGUI(rawEndpointPath, runtime.Agent);
        MapStaticFiles(app, apiBasePath, rawEndpointPath);
    }

    private static void MapApi(WebApplication app, string apiBasePath, RunService runService, AGUISessionService sessionService)
    {
        app.MapGet($"{apiBasePath}/sessions", async (CancellationToken cancellationToken) =>
        {
            var sessions = await sessionService.ListAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(sessions);
        });

        app.MapPost($"{apiBasePath}/sessions", async (CreateAGUISessionRequest request, CancellationToken cancellationToken) =>
        {
            var session = await sessionService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return Results.Created($"{apiBasePath}/sessions/{session.Id}", session);
        });

        app.MapGet($"{apiBasePath}/sessions/{{sessionId}}", async (string sessionId, CancellationToken cancellationToken) =>
        {
            var session = await sessionService.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
            return session is null ? Results.NotFound() : Results.Ok(session);
        });

        app.MapPatch($"{apiBasePath}/sessions/{{sessionId}}", async (string sessionId, UpdateAGUISessionRequest request, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Title)] = ["Title is required."],
                });
            }

            var session = await sessionService.UpdateTitleAsync(sessionId, request.Title, cancellationToken).ConfigureAwait(false);
            return session is null ? Results.NotFound() : Results.Ok(session);
        });

        app.MapDelete($"{apiBasePath}/sessions/{{sessionId}}", async (string sessionId, CancellationToken cancellationToken) =>
        {
            var deleted = await sessionService.DeleteAsync(sessionId, cancellationToken).ConfigureAwait(false);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        app.MapPost($"{apiBasePath}/runs", async (CreateAGUIRunRequest request, CancellationToken cancellationToken) =>
        {
            try
            {
                var run = await runService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
                return Results.Created(string.Concat(apiBasePath, "/runs/", run.Id), run);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [ex.Message],
                });
            }
        });

        app.MapGet($"{apiBasePath}/runs/{{runId}}", (string runId) =>
        {
            var run = runService.Get(runId);
            return run is null ? Results.NotFound() : Results.Ok(run);
        });

        app.MapPost($"{apiBasePath}/runs/{{runId}}/cancel", (string runId) =>
        {
            var run = runService.Get(runId);
            if (run is null)
            {
                return Results.NotFound();
            }

            runService.Cancel(runId);
            return Results.Accepted(string.Concat(apiBasePath, "/runs/", runId), run);
        });

        app.MapGet($"{apiBasePath}/runs/{{runId}}/changes", async (string runId, CancellationToken cancellationToken) =>
        {
            var changes = await runService.GetChangesAsync(runId, cancellationToken).ConfigureAwait(false);
            return changes is null ? Results.NotFound() : Results.Ok(changes);
        });

        app.MapGet($"{apiBasePath}/runs/{{runId}}/diff", async (string runId, string? path, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["path"] = ["Query string parameter 'path' is required."],
                });
            }

            var run = runService.Get(runId);
            if (run is null)
            {
                return Results.NotFound();
            }

            var diff = await runService.GetDiffAsync(runId, path, cancellationToken).ConfigureAwait(false);
            return diff is null ? Results.NotFound() : Results.Ok(diff);
        });

        app.MapGet($"{apiBasePath}/runs/{{runId}}/events", async (HttpContext context, string runId, CancellationToken cancellationToken) =>
        {
            var lastEventId = ReadLastEventId(context.Request);
            var stream = runService.StreamEventsAsync(runId, lastEventId, cancellationToken);
            if (stream is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no";
            context.Response.ContentType = "text/event-stream";

            await foreach (var @event in stream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var payload = JsonSerializer.Serialize(@event, JsonSerializerOptions);
                await context.Response.WriteAsync(string.Concat("id: ", @event.Sequence.ToString(), "\n"), cancellationToken).ConfigureAwait(false);
                await context.Response.WriteAsync(string.Concat("event: ", @event.Type, "\n"), cancellationToken).ConfigureAwait(false);
                await context.Response.WriteAsync($"data: {payload}\n\n", cancellationToken).ConfigureAwait(false);
                await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        });
    }

    private static void MapStaticFiles(WebApplication app, string apiBasePath, string rawEndpointPath)
    {
        var webRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (Directory.Exists(webRootPath))
        {
            PhysicalFileProvider fileProvider = new(webRootPath);
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = fileProvider,
            });
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
                ContentTypeProvider = new FileExtensionContentTypeProvider(),
            });
        }

        app.MapFallback(async context =>
        {
            var requestPath = NormalizePath(context.Request.Path.Value);
            if (requestPath.StartsWith(apiBasePath, StringComparison.OrdinalIgnoreCase) ||
                requestPath.StartsWith(rawEndpointPath, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var indexFilePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");
            if (!File.Exists(indexFilePath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(indexFilePath, context.RequestAborted).ConfigureAwait(false);
        });
    }

    private static long? ReadLastEventId(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Last-Event-ID", out var values))
        {
            return null;
        }

        return long.TryParse(values.ToString(), out var value) ? value : null;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/")
        {
            return "/";
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/');
    }
}

