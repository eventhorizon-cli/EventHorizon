using EventHorizon.Configuration;
using EventHorizon.Pricing;
using EventHorizon.Providers;

namespace EventHorizon.EntryPoints;

internal sealed class EventHorizonApplication : IEventHorizonApplication
{
    private readonly AppOptions _options;
    private readonly EffectiveCommandOptions _command;
    private readonly IModelPriceCatalogService _priceService;
    private readonly IEventHorizonRuntime _runtime;
    private readonly IAGUIServerRunner _aguiServerRunner;

    public EventHorizonApplication(
        AppOptions options,
        EffectiveCommandOptions command,
        IModelPriceCatalogService priceService,
        IEventHorizonRuntime runtime,
        IAGUIServerRunner aguiServerRunner)
    {
        _options = options;
        _command = command;
        _priceService = priceService;
        _runtime = runtime;
        _aguiServerRunner = aguiServerRunner;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _ = LoadPricingInBackgroundAsync(cancellationToken);

        var task = _command.Command switch
        {
            "serve" => _aguiServerRunner.RunAsync(_options, _runtime, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unsupported startup mode '{_command.Command}'. Only '{EffectiveCommandOptions.StartupMode}' is supported."),
        };

        await task;
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
