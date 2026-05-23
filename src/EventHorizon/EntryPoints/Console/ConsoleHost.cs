using System.Text;
using EventHorizon.Commands;
using EventHorizon.Execution;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using Microsoft.Extensions.AI;

namespace EventHorizon.EntryPoints.Console;

public sealed class ConsoleHost
{
    private readonly IEventHorizonRuntime _runtime;
    private readonly QueryEngine _queryEngine;
    private readonly ISlashCommandService _slashCommandService;

    public ConsoleHost(
        IEventHorizonRuntime runtime,
        ModelPriceCatalog catalog,
        ISessionUsageTrackerFactory usageTrackerFactory,
        IQueryEngineFactory queryEngineFactory,
        ISlashCommandService slashCommandService)
    {
        _runtime = runtime;
        _slashCommandService = slashCommandService;
        var usageTracker = usageTrackerFactory.Create(catalog, runtime.ModelName);
        _queryEngine = queryEngineFactory.Create(runtime, usageTracker);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        RenderBanner();

        while (!cancellationToken.IsCancellationRequested)
        {
            System.Console.Write("eventhorizon> ");
            string? input = System.Console.ReadLine();
            if (input is null)
            {
                System.Console.WriteLine();
                break;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (await TryHandleSlashCommandAsync(input, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            await RunPromptAsync(input, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task RunSingleAsync(string prompt, CancellationToken cancellationToken)
        => RunPromptAsync(prompt, cancellationToken);

    private async Task<bool> TryHandleSlashCommandAsync(string input, CancellationToken cancellationToken)
    {
        var result = await _slashCommandService
            .TryExecuteAsync(input, _runtime, _queryEngine, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Handled)
        {
            return false;
        }

        if (result.ExitRequested)
        {
            throw new OperationCanceledException("Exit requested.", cancellationToken);
        }

        return true;
    }

    private async Task RunPromptAsync(string prompt, CancellationToken cancellationToken)
    {
        var assistant = new StringBuilder();
        await foreach (var evt in _queryEngine.SubmitAsync(prompt, cancellationToken))
        {
            if (evt.Kind == QueryEventKind.AssistantDelta)
            {
                assistant.Append(evt.Text);
                System.Console.Write(evt.Text);
                continue;
            }

            if (evt.Kind == QueryEventKind.Completed)
            {
                if (assistant.Length == 0 && !string.IsNullOrWhiteSpace(evt.Text))
                {
                    System.Console.Write(evt.Text);
                }

                System.Console.WriteLine();
                System.Console.WriteLine();
                System.Console.WriteLine(BuildUsageLine(evt.Usage, evt.CostUsd));
            }
        }
    }

    private void RenderBanner()
    {
        System.Console.WriteLine($"{_runtime.ContextSnapshot.CurrentDate}");
        System.Console.WriteLine($"Model: {_runtime.ModelName}");
        System.Console.WriteLine($"Workspace: {_runtime.ContextSnapshot.WorkspaceRoot}");
        System.Console.WriteLine("Type a prompt or use /help.");
        System.Console.WriteLine();
    }

    private static string BuildUsageLine(UsageDetails? usage, decimal? costUsd)
    {
        if (usage is null)
        {
            return "No usage data returned by the model provider.";
        }

        var input = usage.InputTokenCount ?? usage.InputTextTokenCount ?? 0;
        var output = usage.OutputTokenCount ?? usage.OutputTextTokenCount ?? 0;
        var total = usage.TotalTokenCount ?? input + output;
        return costUsd is decimal cost
            ? $"tokens in/out/total = {input}/{output}/{total} · estimated cost {cost:F6} USD"
            : $"tokens in/out/total = {input}/{output}/{total}";
    }
}


