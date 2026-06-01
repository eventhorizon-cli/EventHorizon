using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace EventHorizon.Protocols.Mcp;

public sealed class McpToolConnector
{
    public async Task<(IReadOnlyList<AITool> Tools, IReadOnlyList<IAsyncDisposable> Resources)> ConnectAsync(IReadOnlyList<Configuration.McpServerOptions> servers, CancellationToken cancellationToken)
    {
        List<AITool> tools = [];
        List<IAsyncDisposable> resources = [];

        foreach (var server in servers)
        {
            if (string.IsNullOrWhiteSpace(server.Url))
            {
                throw new InvalidOperationException($"MCP server '{server.Name}' is missing a URL.");
            }

            if (!Uri.TryCreate(server.Url, UriKind.Absolute, out var endpoint) ||
                (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException($"MCP server '{server.Name}' has an invalid HTTP URL: {server.Url}");
            }

            var client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = string.IsNullOrWhiteSpace(server.Name) ? endpoint.Host : server.Name,
                Endpoint = endpoint,
                AdditionalHeaders = new Dictionary<string, string>(server.Headers, StringComparer.OrdinalIgnoreCase),
            }), cancellationToken: cancellationToken).ConfigureAwait(false);

            resources.Add(client);
            tools.AddRange(await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false));
        }

        return (tools, resources);
    }
}
