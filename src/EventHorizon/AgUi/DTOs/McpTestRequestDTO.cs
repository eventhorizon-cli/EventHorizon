using EventHorizon.Configuration;

namespace EventHorizon.AGUI.DTOs;

public sealed class McpTestRequestDTO
{
    public McpServerOptions Server { get; set; } = new();
}

public sealed class McpTestResponseDTO
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public IReadOnlyList<string> Tools { get; set; } = Array.Empty<string>();
}
