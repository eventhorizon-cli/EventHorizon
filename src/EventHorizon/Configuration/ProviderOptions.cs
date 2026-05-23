namespace EventHorizon.Configuration;

public sealed class ProviderOptions
{
    public string Type { get; set; } = "openai";
    public string? Model { get; set; }
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string? Deployment { get; set; }
    public bool UseDefaultAzureCredential { get; set; } = true;
}
