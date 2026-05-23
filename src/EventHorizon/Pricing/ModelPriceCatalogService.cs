using Microsoft.Extensions.Options;

namespace EventHorizon.Pricing;

public sealed class ModelPriceCatalogService : IModelPriceCatalogService
{
    private readonly Configuration.PricingOptions _options;
    private readonly HttpClient _httpClient;

    public ModelPriceCatalogService(IOptions<Configuration.AppOptions> options, HttpClient httpClient)
    {
        _options = options.Value.Pricing;
        _httpClient = httpClient;
    }

    public async Task<ModelPriceCatalog> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var json = await LoadOrRefreshCoreAsync(refresh: _options.RefreshOnStartup, cancellationToken).ConfigureAwait(false);
        return ModelPriceCatalog.FromJson(json);
    }

    public async Task<int> RefreshAsync(CancellationToken cancellationToken)
    {
        var json = await LoadOrRefreshCoreAsync(refresh: true, cancellationToken).ConfigureAwait(false);
        return ModelPriceCatalog.FromJson(json).Entries.Count;
    }

    private async Task<string> LoadOrRefreshCoreAsync(bool refresh, CancellationToken cancellationToken)
    {
        var cachePath = Path.GetFullPath(_options.CachePath ?? throw new InvalidOperationException("Pricing cache path is required."));
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

        if (refresh || !File.Exists(cachePath))
        {
            using var response = await _httpClient.GetAsync(_options.CatalogUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(cachePath, payload, cancellationToken).ConfigureAwait(false);
        }

        return await File.ReadAllTextAsync(cachePath, cancellationToken).ConfigureAwait(false);
    }
}

