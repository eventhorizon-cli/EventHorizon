namespace EventHorizon.Configuration;

public sealed class ProviderTestRequest
{
    public string Name { get; set; } = string.Empty;

    public ProviderOptions Provider { get; set; } = new();
}

public sealed class ProviderTestResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public IReadOnlyList<string> Models { get; set; } = Array.Empty<string>();
}

