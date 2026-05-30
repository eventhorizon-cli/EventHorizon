using EventHorizon.AGUI.DTOs;

namespace EventHorizon.Configuration;

public interface IMcpService
{
    Task<McpTestResponseDTO> TestAsync(McpTestRequestDTO request, CancellationToken cancellationToken);
}
