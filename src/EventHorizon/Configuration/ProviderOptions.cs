namespace EventHorizon.Configuration;

public sealed class ProviderOptions
{
    public string? Name { get; set; }

    public string? Type { get; set; }

    public string? Model { get; set; }

    public List<string> Models { get; set; } = [];

    public string? ApiKey { get; set; }

    public string? Endpoint { get; set; }

    public string? Deployment { get; set; }

    public bool UseDefaultAzureCredential { get; set; } = true;
}
