using EventHorizon.DTOs;
using EventHorizon.Protocols.Mcp;

namespace EventHorizon.Configuration;

internal sealed class McpService : IMcpService
{
    private readonly McpToolConnector _mcpToolConnector;

    public McpService(McpToolConnector mcpToolConnector)
    {
        _mcpToolConnector = mcpToolConnector;
    }

    public async Task<McpTestResponseDTO> TestAsync(McpTestRequestDTO request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mcpToolConnector.ConnectAsync([request.Server], cancellationToken).ConfigureAwait(false);
            var tools = result.Tools.Select(static tool => tool.Name).ToArray();
            foreach (var resource in result.Resources)
            {
                await resource.DisposeAsync().ConfigureAwait(false);
            }

            return new McpTestResponseDTO
            {
                Success = true,
                Message = tools.Length == 0 ? "Connected successfully, but no tools were exposed." : "Connected successfully.",
                Tools = tools,
            };
        }
        catch (Exception ex)
        {
            return new McpTestResponseDTO
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(ex.Message) ? "MCP test failed." : ex.Message,
                Tools = Array.Empty<string>(),
            };
        }
    }
}
