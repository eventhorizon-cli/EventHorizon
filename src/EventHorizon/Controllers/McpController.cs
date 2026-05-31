using EventHorizon.Configuration;
using EventHorizon.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.Controllers;

[ApiController]
[Route("api/mcp")]
public sealed class McpController : ControllerBase
{
    private readonly IMcpService _mcpService;

    public McpController(IMcpService mcpService)
    {
        _mcpService = mcpService;
    }

    [HttpPost("test")]
    public async Task<ActionResult<McpTestResponseDTO>> TestAsync(McpTestRequestDTO request, CancellationToken cancellationToken)
        => Ok(await _mcpService.TestAsync(request, cancellationToken).ConfigureAwait(false));
}
