using EventHorizon.Configuration;
using EventHorizon.Pricing;
using Microsoft.Extensions.Options;

namespace EventHorizon.EntryPoints;

internal sealed class EventHorizonApplication : IEventHorizonApplication
{
    private readonly AppOptions _options;
    private readonly IModelPriceCatalogService _priceService;
    private readonly IAGUIServerRunner _aguiServerRunner;

    public EventHorizonApplication(
        IOptions<AppOptions> options,
        IModelPriceCatalogService priceService,
        IAGUIServerRunner aguiServerRunner)
    {
        _options = options.Value;
        _priceService = priceService;
        _aguiServerRunner = aguiServerRunner;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _ = LoadPricingInBackgroundAsync(cancellationToken);
        await _aguiServerRunner.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task LoadPricingInBackgroundAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            await _priceService.RefreshIfNeededAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }
}
