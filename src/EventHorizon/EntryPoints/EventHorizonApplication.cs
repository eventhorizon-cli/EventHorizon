using System;
using EventHorizon.Configuration;
using EventHorizon.EntryPoints.Console;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using Microsoft.Extensions.Options;

namespace EventHorizon.EntryPoints;

internal sealed class EventHorizonApplication : IEventHorizonApplication
{
    private readonly EffectiveCommandOptions _command;
    private readonly AppOptions _options;
    private readonly IEventHorizonRuntime _runtime;
    private readonly IModelPriceCatalogService _priceService;
    private readonly ConsoleHost _consoleHost;
    private readonly TerminalWorkbenchHost _terminalWorkbenchHost;

    public EventHorizonApplication(
        EffectiveCommandOptions command,
        IOptions<AppOptions> options,
        IEventHorizonRuntime runtime,
        IModelPriceCatalogService priceService,
        ConsoleHost consoleHost,
        TerminalWorkbenchHost terminalWorkbenchHost)
    {
        _command = command;
        _options = options.Value;
        _runtime = runtime;
        _priceService = priceService;
        _consoleHost = consoleHost;
        _terminalWorkbenchHost = terminalWorkbenchHost;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _ = LoadPricingInBackgroundAsync(_terminalWorkbenchHost, cancellationToken);

        var task = _command.Command switch
        {
            "run" => _consoleHost.RunSingleAsync(_command.Prompt, cancellationToken).ConfigureAwait(false),
            "chat" => _consoleHost.RunAsync(cancellationToken).ConfigureAwait(false),
            _ => _terminalWorkbenchHost.RunAsync(cancellationToken).ConfigureAwait(false),
        };

        await task;
    }

    private async Task LoadPricingInBackgroundAsync(TerminalWorkbenchHost host, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            await _priceService.RefreshIfNeededAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            host.RuntimeContext.State.AddActivity(
                "pricing",
                "Pricing refresh failed",
                ex.Message);
        }
    }
}
