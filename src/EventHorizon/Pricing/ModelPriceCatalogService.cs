using System.Globalization;
using Microsoft.Extensions.Options;

namespace EventHorizon.Pricing;

public sealed class ModelPriceCatalogService : IModelPriceCatalogService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly Configuration.PricingOptions _options;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private ModelPriceCatalog? _catalog;

    public ModelPriceCatalogService(
        IOptions<Configuration.AppOptions> options,
        HttpClient httpClient)
    {
        _options = options.Value.Pricing;
        _httpClient = httpClient;
    }

    public ModelPriceCatalogService(ModelPriceCatalog catalog)
    {
        _options = new Configuration.PricingOptions();
        _httpClient = null!;
        _catalog = catalog;
    }

    public bool TryGetCatalog(out ModelPriceCatalog? catalog)
    {
        catalog = _catalog;
        return catalog is not null;
    }

    public async Task RefreshIfNeededAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var cacheDirectory = GetCacheDirectory(_options.CachePath);
            var (latestPath, cachedAt) = FindLatestCachedFile(cacheDirectory);

            if (!string.IsNullOrEmpty(latestPath) &&
                cachedAt.HasValue &&
                !IsExpired(cachedAt.Value))
            {
                _catalog = await LoadCatalogFromFileAsync(latestPath, cancellationToken).ConfigureAwait(false);
                return;
            }

            try
            {
                _catalog = await FetchAndCacheAsync(cacheDirectory, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                if (!string.IsNullOrEmpty(latestPath))
                {
                    _catalog = await LoadCatalogFromFileAsync(latestPath, cancellationToken).ConfigureAwait(false);
                    return;
                }

                throw;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static async Task<ModelPriceCatalog> LoadCatalogFromFileAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return ModelPriceCatalog.FromJson(json);
    }

    private async Task<ModelPriceCatalog> FetchAndCacheAsync(
        string cacheDirectory,
        CancellationToken cancellationToken)
    {
        var cachePath = GetCachePathWithTimestamp(cacheDirectory);

        using var response = await _httpClient.GetAsync(_options.CatalogUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        Directory.CreateDirectory(cacheDirectory);
        await File.WriteAllTextAsync(cachePath, payload, cancellationToken).ConfigureAwait(false);

        return ModelPriceCatalog.FromJson(payload);
    }

    private static bool IsExpired(DateTime cachedAtUtc)
        => DateTime.UtcNow - cachedAtUtc > CacheTtl;

    private static string GetCacheDirectory(string? configuredCachePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredCachePath))
        {
            return configuredCachePath;
        }

        return Path.Combine(AppContext.BaseDirectory, "cache");
    }

    private static string GetCachePathWithTimestamp(string cacheDirectory)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ", CultureInfo.InvariantCulture);
        return Path.Combine(cacheDirectory, $"model_prices_and_context_window_{timestamp}.json");
    }

    private static (string path, DateTime? cachedAt) FindLatestCachedFile(string cacheDirectory)
    {
        const string pattern = "model_prices_and_context_window_*.json";

        if (!Directory.Exists(cacheDirectory))
        {
            return (string.Empty, null);
        }

        var files = Directory.GetFiles(cacheDirectory, pattern);
        string? latestFile = null;
        DateTime? latestTime = null;

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var timestampStr = fileName.Split('_').LastOrDefault();

            if (DateTime.TryParseExact(
                    timestampStr,
                    "yyyy-MM-ddTHH-mm-ssZ",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var timestamp))
            {
                if (latestTime is null || timestamp > latestTime.Value)
                {
                    latestTime = timestamp;
                    latestFile = file;
                }
            }
        }

        return (latestFile ?? string.Empty, latestTime);
    }
}
