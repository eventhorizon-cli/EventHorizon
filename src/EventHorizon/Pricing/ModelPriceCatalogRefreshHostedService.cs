using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHorizon.Pricing;

internal sealed class ModelPriceCatalogRefreshHostedService : BackgroundService
{
    private readonly IModelPriceCatalogService _priceService;
    private readonly ILogger<ModelPriceCatalogRefreshHostedService> _logger;

    public ModelPriceCatalogRefreshHostedService(
        IModelPriceCatalogService priceService,
        ILogger<ModelPriceCatalogRefreshHostedService> logger)
    {
        _priceService = priceService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(100, stoppingToken).ConfigureAwait(false);
            await _priceService.RefreshIfNeededAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh the model price catalog.");
        }
    }
}
