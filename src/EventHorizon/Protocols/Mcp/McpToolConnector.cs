using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace EventHorizon.Protocols.Mcp;

public sealed class McpToolConnector
{
    public async Task<(IReadOnlyList<AITool> Tools, IReadOnlyList<IAsyncDisposable> Resources)> ConnectAsync(IReadOnlyList<Configuration.McpServerOptions> servers, CancellationToken cancellationToken)
    {
        List<AITool> tools = [];
        List<IAsyncDisposable> resources = [];

        foreach (var server in servers.Where(static s => s.Enabled))
        {
            var client = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = string.IsNullOrWhiteSpace(server.Name) ? server.Command : server.Name,
                Command = server.Command,
                Arguments = [.. server.Arguments],
            }), cancellationToken: cancellationToken).ConfigureAwait(false);

            resources.Add(client);
            tools.AddRange(await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false));
        }

        return (tools, resources);
    }
}

