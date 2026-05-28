using System.Text.Json;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Workspace;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace EventHorizon.AGUI;

public sealed class RunService
{
    private readonly RunStore _runStore;
    private readonly IEventHorizonRuntime _runtime;
    private readonly IModelPriceCatalogService _priceCatalogService;
    private readonly WorkspaceSnapshotService _workspaceSnapshotService;
    private readonly DiffService _diffService;
    private readonly AGUISessionService _sessionService;
    private readonly AGUIEventMapper _eventMapper;
    private readonly AGUICodeAgentEventMapper _codeAgentEventMapper;
    private readonly ILogger<RunService> _logger;

    public RunService(
        RunStore runStore,
        IEventHorizonRuntime runtime,
        IModelPriceCatalogService priceCatalogService,
        WorkspaceSnapshotService workspaceSnapshotService,
        DiffService diffService,
        AGUISessionService sessionService,
        AGUIEventMapper eventMapper,
        AGUICodeAgentEventMapper codeAgentEventMapper,
        ILogger<RunService> logger)
    {
        _runStore = runStore;
        _runtime = runtime;
        _priceCatalogService = priceCatalogService;
        _workspaceSnapshotService = workspaceSnapshotService;
        _diffService = diffService;
        _sessionService = sessionService;
        _eventMapper = eventMapper;
        _codeAgentEventMapper = codeAgentEventMapper;
        _logger = logger;
    }

    public async Task<AGUIRun> CreateAsync(CreateAGUIRunRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Task))
        {
            throw new ArgumentException("Task is required.", nameof(request));
        }

        var workingDirectory = NormalizeWorkingDirectory(request.WorkingDirectory);
        var beforeSnapshot = await _workspaceSnapshotService.CaptureAsync(cancellationToken).ConfigureAwait(false);
        var run = new AGUIRun
        {
            Id = $"run_{Guid.NewGuid():N}",
            ThreadId = $"thread_{Guid.NewGuid():N}",
            SessionId = request.SessionId,
            Task = request.Task.Trim(),
            WorkingDirectory = workingDirectory,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        run.MarkRunning(AGUIRunStates.Planning);

        if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            var session = await _sessionService.StartRunAsync(request.SessionId, run.Id, run.Task, cancellationToken).ConfigureAwait(false);
            if (session is null)
            {
                throw new ArgumentException($"Session '{request.SessionId}' was not found.", nameof(request));
            }
        }

        var entry = _runStore.Add(new RunStoreEntry(run, beforeSnapshot, new CancellationTokenSource()));
        var options = request.Options?.Clone();
        _ = Task.Run(() => ExecuteRunAsync(entry, options), CancellationToken.None);
        return run;
    }

    public AGUIRun? Get(string runId)
        => _runStore.TryGet(runId, out var entry) && entry is not null ? entry.Run : null;

    public bool Cancel(string runId)
    {
        if (!_runStore.TryGet(runId, out var entry) || entry is null)
        {
            return false;
        }

        entry.Run.SetDetailedStatus("cancelling");
        entry.CancellationTokenSource.Cancel();
        if (!string.IsNullOrWhiteSpace(entry.Run.SessionId))
        {
            _ = _sessionService.RecordRunCancelledAsync(entry.Run.SessionId, CancellationToken.None);
        }

        return true;
    }

    public IAsyncEnumerable<AGUIEventEnvelope>? StreamEventsAsync(string runId, long? afterSequence, CancellationToken cancellationToken)
        => _runStore.TryGet(runId, out var entry) && entry is not null
            ? entry.SubscribeAsync(afterSequence, cancellationToken)
            : null;

    public async Task<IReadOnlyList<FileChange>?> GetChangesAsync(string runId, CancellationToken cancellationToken)
    {
        if (!_runStore.TryGet(runId, out var entry) || entry is null)
        {
            return null;
        }

        var afterSnapshot = await GetAfterSnapshotAsync(entry, cancellationToken).ConfigureAwait(false);
        return _diffService.GetChanges(entry.BeforeSnapshot, afterSnapshot);
    }

    public async Task<FileDiff?> GetDiffAsync(string runId, string path, CancellationToken cancellationToken)
    {
        if (!_runStore.TryGet(runId, out var entry) || entry is null)
        {
            return null;
        }

        var afterSnapshot = await GetAfterSnapshotAsync(entry, cancellationToken).ConfigureAwait(false);
        return _diffService.GetDiff(entry.BeforeSnapshot, afterSnapshot, path);
    }

    private async Task ExecuteRunAsync(RunStoreEntry entry, JsonElement? options)
    {
        var run = entry.Run;
        var context = new AGUIRunExecutionContext();
        var usageTracker = new SessionUsageTracker(_priceCatalogService, _runtime);
        WorkspaceSnapshot? finalSnapshot = null;

        try
        {
            Publish(entry, _eventMapper.CreateRunStarted(run, _runtime.ModelName, run.WorkingDirectory, options));
            Publish(entry, _eventMapper.CreateUserMessage(run));
            Publish(entry, _eventMapper.CreatePlanUpdated(run, BuildPlan(), []));
            Publish(entry, _eventMapper.CreateReasoningSummaryUpdated(run, BuildInitialSummary(run)));
            Publish(entry, _eventMapper.CreateStepStarted(run, context.ExecutionStepId, "Plan and execute task"));

            run.MarkRunning(AGUIRunStates.Executing);
            var session = await _runtime.Agent.CreateSessionAsync(cancellationToken: entry.CancellationTokenSource.Token).ConfigureAwait(false);
            usageTracker.StartTurn();

            await foreach (var update in _runtime.Agent
                               .RunStreamingAsync(BuildMessages(run), session, cancellationToken: entry.CancellationTokenSource.Token)
                               .ConfigureAwait(false))
            {
                usageTracker.ObserveUpdate(update);
                foreach (var @event in _eventMapper.MapStreamingUpdate(run, context, update))
                {
                    Publish(entry, @event);
                    PublishExtensions(entry, run, context, @event);
                }
            }

            foreach (var @event in _eventMapper.CompleteAssistantMessage(run, context))
            {
                Publish(entry, @event);
            }

            Publish(entry, _eventMapper.CreateStepCompleted(run, context.ExecutionStepId, "Plan and execute task"));
            finalSnapshot = await _workspaceSnapshotService.CaptureAsync(CancellationToken.None).ConfigureAwait(false);
            await PublishWorkspaceChangesAsync(entry, run, finalSnapshot).ConfigureAwait(false);
            run.MarkCompleted();
            Publish(entry, _eventMapper.CreatePlanUpdated(run, BuildPlan(), ["Executed the task.", "Captured the final workspace state."]));
            Publish(entry, _eventMapper.CreateReasoningSummaryUpdated(run, BuildCompletedSummary(run)));
            Publish(entry, _eventMapper.CreateRunFinished(run, usageTracker.LastTurnUsage, usageTracker.LastTurnCost.HasPrice ? usageTracker.LastTurnCost.TotalCost : null));
            if (!string.IsNullOrWhiteSpace(run.SessionId))
            {
                var assistantMessage = context.AssistantText.Length == 0 ? null : context.AssistantText.ToString();
                var changedFilesCount = _diffService.GetChanges(entry.BeforeSnapshot, finalSnapshot).Count;
                await _sessionService.RecordRunCompletedAsync(run.SessionId, assistantMessage, changedFilesCount, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            run.MarkCancelled();
            foreach (var @event in _eventMapper.CompleteAssistantMessage(run, context))
            {
                Publish(entry, @event);
            }

            foreach (var @event in _eventMapper.CompleteOpenToolCalls(run, context, "Run cancelled."))
            {
                Publish(entry, @event);
            }

            Publish(entry, _eventMapper.CreateRunCancelled(run));
            Publish(entry, _eventMapper.CreateReasoningSummaryUpdated(run, BuildCancelledSummary(run)));
            if (!string.IsNullOrWhiteSpace(run.SessionId))
            {
                await _sessionService.RecordRunCancelledAsync(run.SessionId, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AG-UI run {RunId} failed.", run.Id);
            run.MarkFailed(ex.Message);
            foreach (var @event in _eventMapper.CompleteAssistantMessage(run, context))
            {
                Publish(entry, @event);
            }

            foreach (var @event in _eventMapper.CompleteOpenToolCalls(run, context, ex.Message))
            {
                Publish(entry, @event);
            }

            Publish(entry, _eventMapper.CreateError(run, ex.Message));
            Publish(entry, _eventMapper.CreateRunFailed(run, ex.Message));
            if (!string.IsNullOrWhiteSpace(run.SessionId))
            {
                await _sessionService.RecordRunFailedAsync(run.SessionId, ex.Message, CancellationToken.None).ConfigureAwait(false);
            }
        }
        finally
        {
            finalSnapshot ??= await _workspaceSnapshotService.CaptureAsync(CancellationToken.None).ConfigureAwait(false);
            entry.Complete(finalSnapshot);
            entry.CancellationTokenSource.Dispose();
        }
    }

    private async Task PublishWorkspaceChangesAsync(RunStoreEntry entry, AGUIRun run, WorkspaceSnapshot finalSnapshot)
    {
        var changes = _diffService.GetChanges(entry.BeforeSnapshot, finalSnapshot);
        if (changes.Count == 0)
        {
            return;
        }

        var stepId = $"step_{Guid.NewGuid():N}";
        Publish(entry, _eventMapper.CreateStepStarted(run, stepId, "Capture workspace changes"));
        var artifact = _eventMapper.CreateChangesArtifact(changes);
        Publish(entry, _eventMapper.CreateArtifactCreated(run, artifact));
        foreach (var @event in _codeAgentEventMapper.CreateChangeEvents(run, changes))
        {
            Publish(entry, @event);
        }

        Publish(entry, _codeAgentEventMapper.CreateDiffGenerated(run, changes));
        Publish(entry, _eventMapper.CreateArtifactUpdated(run, artifact));
        Publish(entry, _eventMapper.CreateStepCompleted(run, stepId, "Capture workspace changes"));
    }

    private void PublishExtensions(RunStoreEntry entry, AGUIRun run, AGUIRunExecutionContext context, AGUIEventEnvelope @event)
    {
        if (@event.ToolCallId is null || !context.ToolCalls.TryGetValue(@event.ToolCallId, out var toolCall))
        {
            return;
        }

        var extensionEvents = @event.Type switch
        {
            "toolCallStart" => _codeAgentEventMapper.CreateToolStartExtensions(run, toolCall),
            "toolCallResult" => _codeAgentEventMapper.CreateToolResultExtensions(run, toolCall, @event.Result?.ToString()),
            _ => [],
        };

        foreach (var extensionEvent in extensionEvents)
        {
            Publish(entry, extensionEvent);
        }
    }

    private static IEnumerable<ChatMessage> BuildMessages(AGUIRun run)
    {
        if (!string.IsNullOrWhiteSpace(run.WorkingDirectory))
        {
            yield return new ChatMessage(
                ChatRole.System,
                $"Preferred working directory for this task: {run.WorkingDirectory}. Use relative paths from that directory when practical.");
        }

        yield return new ChatMessage(ChatRole.User, run.Task);
    }

    private static IReadOnlyList<string> BuildPlan()
        =>
        [
            "Analyze the task safely.",
            "Execute the agent run and stream events.",
            "Capture workspace changes and final status.",
        ];

    private static AGUIReasoningSummary BuildInitialSummary(AGUIRun run)
        => new(
            run.Task,
            BuildPlan(),
            [],
            "Start executing the task.",
            [],
            ["Expose only safe summaries instead of full internal reasoning."]);

    private static AGUIReasoningSummary BuildCompletedSummary(AGUIRun run)
        => new(
            run.Task,
            BuildPlan(),
            ["Executed the task.", "Captured the final workspace state."],
            null,
            [],
            ["Reported the final run state through AG-UI events."]);

    private static AGUIReasoningSummary BuildCancelledSummary(AGUIRun run)
        => new(
            run.Task,
            BuildPlan(),
            [],
            "Wait for a new run request.",
            ["The run was cancelled before completion."],
            ["Stopped streaming once cancellation was requested."]);

    private void Publish(RunStoreEntry entry, AGUIEventEnvelope @event)
        => entry.Publish(@event);

    private async Task<WorkspaceSnapshot> GetAfterSnapshotAsync(RunStoreEntry entry, CancellationToken cancellationToken)
        => entry.FinalSnapshot ?? await _workspaceSnapshotService.CaptureAsync(cancellationToken).ConfigureAwait(false);

    private string? NormalizeWorkingDirectory(string? workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return null;
        }

        var workspaceRoot = _workspaceSnapshotService.WorkspaceRoot;
        var candidate = Path.IsPathRooted(workingDirectory)
            ? Path.GetFullPath(workingDirectory)
            : Path.GetFullPath(Path.Combine(workspaceRoot, workingDirectory));
        var normalizedRoot = workspaceRoot.EndsWith(Path.DirectorySeparatorChar)
            ? workspaceRoot
            : workspaceRoot + Path.DirectorySeparatorChar;
        if (!candidate.Equals(workspaceRoot, StringComparison.Ordinal) &&
            !candidate.StartsWith(normalizedRoot, StringComparison.Ordinal))
        {
            throw new ArgumentException("workingDirectory must stay inside the configured workspace.", nameof(workingDirectory));
        }

        return Path.GetRelativePath(workspaceRoot, candidate).Replace('\\', '/');
    }
}

