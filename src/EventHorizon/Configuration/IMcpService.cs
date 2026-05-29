namespace EventHorizon.Configuration;

public interface IMcpService
{
    Task<McpTestResponse> TestAsync(McpTestRequest request, CancellationToken cancellationToken);
}

