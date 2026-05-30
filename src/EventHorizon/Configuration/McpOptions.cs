namespace EventHorizon.Configuration;

public sealed class McpOptions
{
    public List<McpServerOptions> Servers { get; set; } = [];
}
