using EventHorizon.Configuration;
using EventHorizon.Providers;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventHorizon.EntryPoints;

internal sealed class AguiServerRunner : IAguiServerRunner
{
    public async Task RunAsync(AppOptions options, IEventHorizonRuntime runtime, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHttpClient().AddLogging();
        builder.Services.AddAGUI();

        var app = builder.Build();
        app.Urls.Add(options.Protocol.Url);
        app.MapGet("/", () => Results.Ok(new { name = options.Agent.Name, protocol = "AGUI", endpoint = options.Protocol.Path }));
        app.MapAGUI(options.Protocol.Path, runtime.Agent);
        await app.RunAsync(cancellationToken).ConfigureAwait(false);
    }
}
