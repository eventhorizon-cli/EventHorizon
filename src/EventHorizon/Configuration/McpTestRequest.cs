namespace EventHorizon.Configuration;

public sealed class McpTestRequest
{
    public McpServerOptions Server { get; set; } = new();
}

public sealed class McpTestResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public IReadOnlyList<string> Tools { get; set; } = Array.Empty<string>();
}

