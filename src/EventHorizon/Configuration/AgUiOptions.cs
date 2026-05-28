namespace EventHorizon.Configuration;

public sealed class AGUIOptions
{
    public string ApiBasePath { get; set; } = "/api";

    public string RawEndpointPath { get; set; } = "/agui";

    public List<string> Urls { get; set; } = ["http://127.0.0.1:9527"];
}

