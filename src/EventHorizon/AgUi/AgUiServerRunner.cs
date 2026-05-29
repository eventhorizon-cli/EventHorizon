using System.Text.Json.Serialization;
using EventHorizon.Configuration;
using EventHorizon.EntryPoints;
using EventHorizon.Providers;
using EventHorizon.Workspace;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventHorizon.AGUI;

public sealed class AGUIServerRunner : IAGUIServerRunner
{
    private readonly RunService _runService;
    private readonly IAGUISessionService _sessionService;
    private readonly WorkspaceContext _workspaceContext;
    private readonly ILogger<AGUIServerRunner> _logger;

    public AGUIServerRunner(RunService runService, IAGUISessionService sessionService, WorkspaceContext workspaceContext, ILogger<AGUIServerRunner> logger)
    {
        _runService = runService;
        _sessionService = sessionService;
        _workspaceContext = workspaceContext;
        _logger = logger;
    }

    public async Task RunAsync(AppOptions options, IEventHorizonRuntime runtime, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHttpClient().AddLogging();
        builder.Services.AddSingleton(_runService);
        builder.Services.AddSingleton(_sessionService);
        builder.Services.AddSingleton(_workspaceContext);
        builder.Services.AddSingleton(runtime);
        builder.Services.AddControllers();
        builder.Services.Configure<JsonOptions>(jsonOptions =>
        {
            jsonOptions.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        builder.Services.Configure<MvcJsonOptions>(jsonOptions =>
        {
            jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        builder.Services.AddAGUI();

        var app = builder.Build();
        foreach (var url in options.AgUi.Urls.Distinct())
        {
            app.Urls.Add(url);
        }

        app.MapControllers();
        AGUIEndpoints.Map(app, options.AgUi, runtime);

        _logger.LogInformation(
            "Starting EventHorizon AG-UI server on {Urls}. API base path: {ApiBasePath}. Raw AG-UI endpoint: {RawEndpointPath}.",
            string.Join(", ", options.AgUi.Urls),
            options.AgUi.ApiBasePath,
            options.AgUi.RawEndpointPath);

        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("EventHorizon AG-UI server listening on {Addresses}.", string.Join(", ", app.Urls));

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await app.StopAsync(CancellationToken.None).ConfigureAwait(false);
            await app.DisposeAsync().ConfigureAwait(false);
        }
    }
}

