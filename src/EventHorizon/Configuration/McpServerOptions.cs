namespace EventHorizon.Configuration;

public sealed class McpServerOptions
{
    public bool Enabled { get; set; } = true;

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
