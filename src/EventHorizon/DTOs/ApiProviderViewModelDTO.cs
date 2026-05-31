namespace EventHorizon.DTOs;

public sealed class ApiProviderViewModelDTO
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string? Model { get; set; }

    public IReadOnlyList<string> Models { get; set; } = Array.Empty<string>();

    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public string? Deployment { get; set; }

    public bool UseDefaultAzureCredential { get; set; }
}
