using System.Text.Json;
using System.Text.Json.Serialization;
using EventHorizon.Configuration;
using EventHorizon.EntryPoints;
using EventHorizon.Providers;
using EventHorizon.Workspace;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace EventHorizon.AGUI;

public static class AGUIEndpoints
{
    public static void Map(WebApplication app, AGUIOptions options, IEventHorizonRuntime runtime)
    {
        var apiBasePath = NormalizePath(options.ApiBasePath);
        var rawEndpointPath = NormalizePath(options.RawEndpointPath);
        try
        {
            app.MapAGUI(rawEndpointPath, runtime.Agent);
        }
        catch (InvalidOperationException)
        {
            app.Map(rawEndpointPath, static async context =>
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Agent runtime is not ready. Configure a valid provider and try again.",
                }, context.RequestAborted).ConfigureAwait(false);
            });
        }

        MapStaticFiles(app, apiBasePath, rawEndpointPath);

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var initializer = app.Services.GetRequiredService<IEventHorizonRuntimeInitializer>();
                await initializer.InitializeAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            });
        });
    }

    private static void MapStaticFiles(WebApplication app, string apiBasePath, string rawEndpointPath)
    {
        var webRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        IFileProvider fileProvider = new ManifestEmbeddedFileProvider(typeof(AGUIEndpoints).Assembly, "wwwroot");
        if (Directory.Exists(webRootPath))
        {
            fileProvider = new CompositeFileProvider(new PhysicalFileProvider(webRootPath), fileProvider);
        }

        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = fileProvider,
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            ContentTypeProvider = new FileExtensionContentTypeProvider(),
        });

        app.MapFallback(async context =>
        {
            var requestPath = NormalizePath(context.Request.Path.Value);
            if (requestPath.StartsWith(apiBasePath, StringComparison.OrdinalIgnoreCase) ||
                requestPath.StartsWith(rawEndpointPath, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var indexFile = fileProvider.GetFileInfo("index.html");
            if (!indexFile.Exists)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await using var stream = indexFile.CreateReadStream();
            await stream.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
        });
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
