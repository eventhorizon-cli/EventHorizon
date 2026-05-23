using EventHorizon.Providers;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace EventHorizon.EntryPoints;

internal sealed class McpServerRunner : IMcpServerRunner
{
    public async Task RunAsync(IEventHorizonRuntime runtime, CancellationToken cancellationToken)
    {
        var tool = McpServerTool.Create(runtime.Agent.AsAIFunction());
        var builder = Host.CreateEmptyApplicationBuilder(settings: null);
        builder.Services.AddMcpServer().WithStdioServerTransport().WithTools([tool]);
        using var host = builder.Build();
        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }
}
