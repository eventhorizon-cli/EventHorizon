namespace EventHorizon.Configuration;

public sealed class AGUIOptions
{
    public string ApiBasePath { get; set; } = "/api";

    public string RawEndpointPath { get; set; } = "/agui";

    public HashSet<string>? Urls { get; set; }
}

