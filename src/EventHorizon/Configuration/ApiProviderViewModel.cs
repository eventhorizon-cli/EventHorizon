namespace EventHorizon.Configuration;

public sealed class ApiProviderViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string? Model { get; set; }

    public IReadOnlyList<string> Models { get; set; } = Array.Empty<string>();

    public string? Endpoint { get; set; }

    public string? ApiKeyMasked { get; set; }

    public string? Deployment { get; set; }

    public bool UseDefaultAzureCredential { get; set; }
}

