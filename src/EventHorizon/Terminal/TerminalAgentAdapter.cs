using System.Runtime.CompilerServices;
using EventHorizon.Execution;
using EventHorizon.Providers;
using EventHorizon.Terminal.Events;
using EventHorizon.Terminal.Models;
using EventHorizon.Workspace;

namespace EventHorizon.Terminal;

public interface ITerminalAgentAdapter
{
    IAsyncEnumerable<TerminalAgentEvent> SendAsync(string userInput, CancellationToken cancellationToken);
}

public sealed class TerminalAgentAdapter : ITerminalAgentAdapter
{
    private readonly QueryEngine _queryEngine;
    private readonly IEventHorizonRuntime _runtime;
    private readonly WorkspaceService _workspaceService;

    public TerminalAgentAdapter(QueryEngine queryEngine, IEventHorizonRuntime runtime, WorkspaceService workspaceService)
    {
        _queryEngine = queryEngine;
        _runtime = runtime;
        _workspaceService = workspaceService;
    }

    public async IAsyncEnumerable<TerminalAgentEvent> SendAsync(string userInput, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new StatusChanged(TerminalRunStatus.Thinking, "Submitting prompt");
        yield return new PlanUpdated(
        [
            new TerminalPlanItem { Title = "Analyze request", Status = TerminalPlanItemStatus.Completed },
            new TerminalPlanItem { Title = "Stream assistant response", Status = TerminalPlanItemStatus.InProgress },
            new TerminalPlanItem { Title = "Refresh diff and context panes", Status = TerminalPlanItemStatus.Pending },
        ]);

        string? currentToolId = null;
        var toolCounter = 0;

        await foreach (var queryEvent in _queryEngine.SubmitAsync(userInput, cancellationToken).ConfigureAwait(false))
        {
            switch (queryEvent.Kind)
            {
                case QueryEventKind.UserMessage:
                    break;
                case QueryEventKind.ToolCall:
                    toolCounter++;
                    currentToolId = $"tool-{toolCounter:D4}";
                    yield return new StatusChanged(TerminalRunStatus.ToolRunning, queryEvent.Text);
                    yield return new ToolCallStarted(currentToolId, ParseToolName(queryEvent.Text), null, queryEvent.Text);
                    break;
                case QueryEventKind.ToolResult:
                    if (currentToolId is null)
                    {
                        toolCounter++;
                        currentToolId = $"tool-{toolCounter:D4}";
                        yield return new ToolCallStarted(currentToolId, ParseToolName(queryEvent.Text), null, queryEvent.Text);
                    }

                    yield return new ToolCallOutput(currentToolId, queryEvent.Text);
                    yield return new ToolCallFinished(currentToolId, true, null);
                    currentToolId = null;
                    break;
                case QueryEventKind.AssistantDelta:
                    yield return new StatusChanged(TerminalRunStatus.Streaming, "Streaming response");
                    yield return new AssistantDelta(queryEvent.Text);
                    break;
                case QueryEventKind.Completed:
                    yield return new AssistantMessageCompleted(queryEvent.Text, (int?)queryEvent.Usage?.TotalTokenCount, queryEvent.CostUsd);
                    yield return new DiffUpdated(await ReadDiffsAsync(cancellationToken).ConfigureAwait(false));
                    yield return new ContextFilesUpdated(WorkspaceExplorerSnapshotBuilder.Build(_runtime.ContextSnapshot.WorkspaceRoot, focusedPath: null));
                    yield return new PlanUpdated(
                    [
                        new TerminalPlanItem { Title = "Analyze request", Status = TerminalPlanItemStatus.Completed },
                        new TerminalPlanItem { Title = "Stream assistant response", Status = TerminalPlanItemStatus.Completed },
                        new TerminalPlanItem { Title = "Refresh diff and context panes", Status = TerminalPlanItemStatus.Completed },
                    ]);
                    yield return new StatusChanged(TerminalRunStatus.WaitingForInput, "Ready for the next instruction");
                    break;
            }
        }
    }

    private async Task<IReadOnlyList<TerminalDiffItem>> ReadDiffsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var output = await _workspaceService
                .RunShellAsync("git --no-pager status --short | cat", 20, cancellationToken)
                .ConfigureAwait(false);
            return ParseDiffItems(output);
        }
        catch (Exception ex)
        {
            return
            [
                new TerminalDiffItem
                {
                    Path = "git status unavailable",
                    Kind = TerminalDiffKind.Modified,
                    Summary = ex.Message,
                },
            ];
        }
    }

    private static IReadOnlyList<TerminalDiffItem> ParseDiffItems(string output)
    {
        List<TerminalDiffItem> items = [];
        foreach (var rawLine in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawLine.StartsWith("##", StringComparison.Ordinal))
            {
                continue;
            }

            var line = rawLine.Trim();
            if (line.Length < 4)
            {
                continue;
            }

            var status = line[0];
            var path = line[3..].Trim();
            items.Add(new TerminalDiffItem
            {
                Path = path,
                Kind = status switch
                {
                    'A' => TerminalDiffKind.Added,
                    'D' => TerminalDiffKind.Deleted,
                    'R' => TerminalDiffKind.Renamed,
                    _ => TerminalDiffKind.Modified,
                },
                Summary = line[..Math.Min(3, line.Length)].Trim(),
            });
        }

        return items;
    }

    private static string ParseToolName(string value)
        => value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "tool";
}

