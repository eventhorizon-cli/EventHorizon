using EventHorizon.Configuration;
using EventHorizon.EntryPoints.Console;
using EventHorizon.Pricing;

namespace EventHorizon.EntryPoints;

internal sealed class EventHorizonApplication : IEventHorizonApplication
{
    private readonly EffectiveCommandOptions _command;
    private readonly IModelPriceCatalogService _priceService;
    private readonly ConsoleHost _consoleHost;
    private readonly TerminalWorkbenchHost _terminalWorkbenchHost;

    public EventHorizonApplication(
        EffectiveCommandOptions command,
        IModelPriceCatalogService priceService,
        ConsoleHost consoleHost,
        TerminalWorkbenchHost terminalWorkbenchHost)
    {
        _command = command;
        _priceService = priceService;
        _consoleHost = consoleHost;
        _terminalWorkbenchHost = terminalWorkbenchHost;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _ = LoadPricingInBackgroundAsync(cancellationToken);

        var task = _command.Command switch
        {
            "run" => _consoleHost.RunSingleAsync(_command.Prompt, cancellationToken).ConfigureAwait(false),
            "chat" => _consoleHost.RunAsync(cancellationToken).ConfigureAwait(false),
            _ => _terminalWorkbenchHost.RunAsync(cancellationToken).ConfigureAwait(false),
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
