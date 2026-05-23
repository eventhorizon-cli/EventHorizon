namespace EventHorizon.Configuration;

public sealed class PricingOptions
{
    public string CatalogUrl { get; set; } = "https://raw.githubusercontent.com/BerriAI/litellm/main/model_prices_and_context_window.json";
    public string? CachePath { get; set; }
    public bool RefreshOnStartup { get; set; } = true;
}
