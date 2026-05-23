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
    private readonly IEventHorizonRuntimeFactory _runtimeFactory;
    private readonly IModelPriceCatalogService _priceService;
    private readonly IConsoleHostFactory _consoleHostFactory;
    private readonly ITerminalWorkbenchHostFactory _terminalWorkbenchHostFactory;
    private readonly IRemoteEventHorizonRuntimeFactory _remoteRuntimeFactory;
    private readonly IAguiServerRunner _aguiServerRunner;
    private readonly IMcpServerRunner _mcpServerRunner;

    public EventHorizonApplication(
        EffectiveCommandOptions command,
        IOptions<AppOptions> options,
        IEventHorizonRuntimeFactory runtimeFactory,
        IModelPriceCatalogService priceService,
        IConsoleHostFactory consoleHostFactory,
        ITerminalWorkbenchHostFactory terminalWorkbenchHostFactory,
        IRemoteEventHorizonRuntimeFactory remoteRuntimeFactory,
        IAguiServerRunner aguiServerRunner,
        IMcpServerRunner mcpServerRunner)
    {
        _command = command;
        _options = options.Value;
        _runtimeFactory = runtimeFactory;
        _priceService = priceService;
        _consoleHostFactory = consoleHostFactory;
        _terminalWorkbenchHostFactory = terminalWorkbenchHostFactory;
        _remoteRuntimeFactory = remoteRuntimeFactory;
        _aguiServerRunner = aguiServerRunner;
        _mcpServerRunner = mcpServerRunner;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        return _command.Command switch
        {
            "serve" => await RunAguiServerAsync(cancellationToken).ConfigureAwait(false),
            "client" => await RunAguiClientAsync(cancellationToken).ConfigureAwait(false),
            "mcp-server" => await RunMcpServerAsync(cancellationToken).ConfigureAwait(false),
            "prices-refresh" => await RefreshPricesAsync(cancellationToken).ConfigureAwait(false),
            "run" => await RunLocalAsync(_command.Prompt, cancellationToken).ConfigureAwait(false),
            _ => await RunLocalAsync(prompt: null, cancellationToken).ConfigureAwait(false),
        };
    }

    private async Task<int> RunLocalAsync(string? prompt, CancellationToken cancellationToken)
    {
        await using var runtime = await _runtimeFactory.CreateAsync(_options, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(prompt))
        {
            var catalog = await _priceService.GetCatalogAsync(cancellationToken).ConfigureAwait(false);
            var host = _terminalWorkbenchHostFactory.Create(runtime, _options, catalog);

            _ = LoadPricingInBackgroundAsync(host, cancellationToken);

            await host.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var catalog = await _priceService.GetCatalogAsync(cancellationToken).ConfigureAwait(false);
            var host = _consoleHostFactory.Create(runtime, catalog);
            await host.RunSingleAsync(prompt, cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }

    private async Task LoadPricingInBackgroundAsync(TerminalWorkbenchHost host, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            await _priceService.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            host.RuntimeContext.State.AddActivity(
                "pricing",
                "Pricing refresh failed",
                ex.Message);
        }
    }

    private async Task<int> RunAguiClientAsync(CancellationToken cancellationToken)
    {
        var catalog = await _priceService.GetCatalogAsync(cancellationToken).ConfigureAwait(false);
        await using var runtime = _remoteRuntimeFactory.Create(_options);

        if (string.IsNullOrWhiteSpace(_command.Prompt))
        {
            var host = _terminalWorkbenchHostFactory.Create(runtime, _options, catalog);
            await host.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var host = _consoleHostFactory.Create(runtime, catalog);
            await host.RunSingleAsync(_command.Prompt, cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }

    private async Task<int> RunAguiServerAsync(CancellationToken cancellationToken)
    {
        await using var runtime = await _runtimeFactory.CreateAsync(_options, cancellationToken).ConfigureAwait(false);
        await _aguiServerRunner.RunAsync(_options, runtime, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private async Task<int> RunMcpServerAsync(CancellationToken cancellationToken)
    {
        await using var runtime = await _runtimeFactory.CreateAsync(_options, cancellationToken).ConfigureAwait(false);
        await _mcpServerRunner.RunAsync(runtime, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private async Task<int> RefreshPricesAsync(CancellationToken cancellationToken)
    {
        var count = await _priceService.RefreshAsync(cancellationToken).ConfigureAwait(false);
        System.Console.WriteLine($"Cached {count} pricing entries.");
        return 0;
    }
}

