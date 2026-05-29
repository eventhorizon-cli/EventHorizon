namespace EventHorizon.Configuration;

public sealed class McpServerOptions
{
    public string Name { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;

    public string[] Arguments { get; set; } = [];

    public string? Url { get; set; }

    public Dictionary<string, string> EnvironmentVariables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Enabled { get; set; } = true;
}
