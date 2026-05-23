using System.Text.Json;
using Microsoft.Extensions.AI;

namespace EventHorizon.Pricing;

public sealed class ModelPriceCatalog
{
    private readonly Dictionary<string, ModelCatalogEntry> _entries;

    public ModelPriceCatalog(Dictionary<string, ModelCatalogEntry> entries)
    {
        _entries = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string key, ModelCatalogEntry value) in entries)
        {
            _entries[key] = value;
        }
    }

    public IReadOnlyDictionary<string, ModelCatalogEntry> Entries => _entries;

    public static ModelPriceCatalog FromJson(string json)
    {
        Dictionary<string, ModelCatalogEntry>? entries = JsonSerializer.Deserialize(
            json,
            Configuration.EventHorizonJsonContext.Default.DictionaryStringModelCatalogEntry);

        return new ModelPriceCatalog(entries ?? []);
    }

    public bool TryGetEntry(string modelName, out ModelCatalogEntry entry)
    {
        if (_entries.TryGetValue(modelName, out entry!))
        {
            return true;
        }

        var normalized = modelName.Trim();
        if (_entries.TryGetValue(normalized, out entry!))
        {
            return true;
        }

        var withoutProviderPrefix = normalized.Replace("azure/", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (_entries.TryGetValue(withoutProviderPrefix, out entry!))
        {
            return true;
        }

        var withoutSlashVersion = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? normalized;
        if (_entries.TryGetValue(withoutSlashVersion, out entry!))
        {
            return true;
        }

        entry = new ModelCatalogEntry();
        return false;
    }

    public UsageCost EstimateCost(string modelName, UsageDetails usage)
    {
        if (!TryGetEntry(modelName, out var entry))
        {
            return UsageCost.Unknown(usage);
        }

        var inputTokens = usage.InputTokenCount ?? usage.InputTextTokenCount ?? 0;
        var outputTokens = usage.OutputTokenCount ?? usage.OutputTextTokenCount ?? 0;
        var cachedInputTokens = usage.CachedInputTokenCount ?? 0;

        var inputCost = (decimal)inputTokens * (decimal)entry.InputCostPerToken;
        var outputCost = (decimal)outputTokens * (decimal)entry.OutputCostPerToken;
        var cachedInputCost = (decimal)cachedInputTokens * (decimal)(entry.CacheReadInputCostPerToken ?? entry.InputCostPerToken);

        return new UsageCost(
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            CachedInputTokens: cachedInputTokens,
            InputCost: inputCost,
            OutputCost: outputCost,
            CachedInputCost: cachedInputCost,
            TotalCost: inputCost + outputCost + cachedInputCost,
            HasPrice: true,
            Currency: "USD");
    }

    public sealed class ModelCatalogEntry
    {
        [System.Text.Json.Serialization.JsonPropertyName("input_cost_per_token")]
        public double InputCostPerToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("output_cost_per_token")]
        public double OutputCostPerToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("cache_read_input_token_cost_per_token")]
        public double? CacheReadInputCostPerToken { get; set; }
    }
}

public readonly record struct UsageCost(
    long InputTokens,
    long OutputTokens,
    long CachedInputTokens,
    decimal InputCost,
    decimal OutputCost,
    decimal CachedInputCost,
    decimal TotalCost,
    bool HasPrice,
    string Currency)
{
    public static UsageCost Unknown(UsageDetails usage) => new(
        InputTokens: usage.InputTokenCount ?? usage.InputTextTokenCount ?? 0,
        OutputTokens: usage.OutputTokenCount ?? usage.OutputTextTokenCount ?? 0,
        CachedInputTokens: usage.CachedInputTokenCount ?? 0,
        InputCost: 0,
        OutputCost: 0,
        CachedInputCost: 0,
        TotalCost: 0,
        HasPrice: false,
        Currency: "USD");
}

