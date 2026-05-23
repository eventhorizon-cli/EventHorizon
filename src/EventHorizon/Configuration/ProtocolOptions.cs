namespace EventHorizon.Configuration;

public sealed class ProtocolOptions
{
    public string Url { get; set; } = "http://127.0.0.1:8787";
    public string Path { get; set; } = "/agui";
    public string ClientUrl { get; set; } = "http://127.0.0.1:8787/agui";
}
