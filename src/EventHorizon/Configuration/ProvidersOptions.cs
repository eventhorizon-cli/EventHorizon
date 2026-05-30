namespace EventHorizon.Configuration;

public sealed class ProvidersOptions
{
    public string? CurrentDefaultProvider { get; set; }

    public Dictionary<string, ProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
