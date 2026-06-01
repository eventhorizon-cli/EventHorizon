using System.Text.Json;
using EventHorizon.DTOs;
using EventHorizon.Engine.Events;
using EventHorizon.Engine.Sessions;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Workspace.Diff;
using Microsoft.Extensions.AI;

namespace EventHorizon.Engine.Runs;

public sealed class RunService : IRunService
{
    private static readonly TimeSpan SessionInitializationTimeout = TimeSpan.FromSeconds(30);
    private readonly RunStore _runStore;
    private readonly IModelPriceCatalogService _priceCatalogService;
    private readonly IFileSnapshotService _fileSnapshotService;
    private readonly IFileStateTrackerAccessor _fileStateTrackerAccessor;
    private readonly IDiffService _diffService;
    private readonly ISessionService _sessionService;
    private readonly EventMapper _eventMapper;
    private readonly CodeAgentEventMapper _codeAgentEventMapper;
    private readonly ISessionAgentManager _conversationAgentManager;
    private readonly ILogger<RunService> _logger;

    public RunService(
        RunStore runStore,
        IModelPriceCatalogService priceCatalogService,
        IFileSnapshotService fileSnapshotService,
        IFileStateTrackerAccessor fileStateTrackerAccessor,
        IDiffService diffService,
        ISessionService sessionService,
        EventMapper eventMapper,
        CodeAgentEventMapper codeAgentEventMapper,
        ISessionAgentManager conversationAgentManager,
        ILogger<RunService> logger)
    {
        _runStore = runStore;
        _priceCatalogService = priceCatalogService;
        _fileSnapshotService = fileSnapshotService;
        _fileStateTrackerAccessor = fileStateTrackerAccessor;
        _diffService = diffService;
        _sessionService = sessionService;
        _eventMapper = eventMapper;
        _codeAgentEventMapper = codeAgentEventMapper;
        _conversationAgentManager = conversationAgentManager;
        _logger = logger;
    }

    public async Task<RunDTO> CreateAsync(CreateRunRequestDTO request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Task))
        {
            throw new ArgumentException("Task is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            throw new ArgumentException("SessionId is required.", nameof(request));
        }

        var sessionDocument = await _sessionService.GetDocumentAsync(request.SessionId, cancellationToken).ConfigureAwait(false);
        if (sessionDocument is null)
        {
            throw new ArgumentException($"Session '{request.SessionId}' was not found.", nameof(request));
        }

        var normalizedWorkingDirectory = NormalizeWorkingDirectory(sessionDocument.WorkspaceRoot);
        var run = new Run
        {
            Id = $"run_{Guid.NewGuid():N}",
            ThreadId = $"thread_{Guid.NewGuid():N}",
            SessionId = sessionDocument.Id,
            Task = request.Task.Trim(),
            WorkingDirectory = normalizedWorkingDirectory,
            ProviderName = sessionDocument.ProviderName,
            Model = sessionDocument.Model,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        run.MarkRunning(RunStates.Planning);

        var session = await _sessionService.StartRunAsync(sessionDocument.Id, run.Id, run.Task, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            throw new ArgumentException($"Session '{request.SessionId}' was not found.", nameof(request));
        }

        var fileStateTracker = new FileStateTracker(run.Id, run.SessionId, _fileSnapshotService, _diffService);
        var entry = _runStore.Add(new RunStoreEntry(run, fileStateTracker, new CancellationTokenSource()));
        var options = request.Options?.Clone();
        _ = Task.Run(() => ExecuteRunAsync(entry, options), CancellationToken.None);
        return MapRun(run);
    }

    public RunDTO? Get(string sessionId, string runId)
    {
        if (!TryGetRun(sessionId, runId, out var entry))
        {
            return null;
        }

        return MapRun(entry.Run);
    }

    public bool Cancel(string sessionId, string runId)
    {
        if (!TryGetRun(sessionId, runId, out var entry))
        {
            return false;
        }

        if (!string.Equals(entry.Run.Status, RunStates.Running, StringComparison.Ordinal))
        {
            return false;
        }

        entry.Run.SetDetailedStatus("cancelling");
        try
        {
            entry.CancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(entry.Run.SessionId))
        {
            _conversationAgentManager.Invalidate(entry.Run.SessionId, CancellationToken.None);
            _ = _sessionService.RecordRunCancelledAsync(entry.Run.SessionId, CancellationToken.None);
        }

        return true;
    }

    public IAsyncEnumerable<EventEnvelope>? StreamEventsAsync(string sessionId, string runId, long? afterSequence, CancellationToken cancellationToken)
        => TryGetRun(sessionId, runId, out var entry)
            ? entry.SubscribeAsync(afterSequence, cancellationToken)
            : null;

    public IReadOnlyList<FileChange>? GetChanges(string sessionId, string runId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryGetRun(sessionId, runId, out var entry))
        {
            return null;
        }

        return entry.FileStateTracker.GetChanges();
    }

    public FileDiff? GetDiff(string sessionId, string runId, string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryGetRun(sessionId, runId, out var entry))
        {
            return null;
        }

        return entry.FileStateTracker.GetDiff(path);
    }

    private async Task ExecuteRunAsync(RunStoreEntry entry, JsonElement? options)
    {
        var run = entry.Run;
        var context = new RunExecutionContext();

        try
        {
            var sessionId = run.SessionId ?? throw new InvalidOperationException("Run session id is required.");
            var sessionDocument = await _sessionService.GetDocumentAsync(sessionId, CancellationToken.None).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");
            var conversationRuntime = await WithSessionInitializationTimeout(
                _conversationAgentManager.GetOrCreateAsync(sessionDocument, entry.CancellationTokenSource.Token),
                "Timed out while initializing the conversation agent.",
                entry.CancellationTokenSource.Token).ConfigureAwait(false);

            var agent = conversationRuntime.Agent;
            var session = conversationRuntime.Session;
            var resolvedProvider = conversationRuntime.ResolvedProvider;
            var usageTracker = new SessionUsageTracker(_priceCatalogService, resolvedProvider.Model);

            Publish(entry, _eventMapper.CreateRunStarted(run, resolvedProvider.Model, run.WorkingDirectory, options));
            Publish(entry, _eventMapper.CreateUserMessage(run));
            Publish(entry, _eventMapper.CreatePlanUpdated(run, BuildPlan(), []));
            Publish(entry, _eventMapper.CreateReasoningSummaryUpdated(run, BuildInitialSummary(run)));
            Publish(entry, _eventMapper.CreateStepStarted(run, context.ExecutionStepId, "Plan and execute task"));

            run.MarkRunning(RunStates.Executing);
            using var fileTrackingScope = _fileStateTrackerAccessor.BeginScope(entry.FileStateTracker);
            usageTracker.StartTurn();

            var messages = !conversationRuntime.WasReused
                ? BuildMessages(run, sessionDocument.Transcript)
                : BuildMessages(run);

            await foreach (var update in agent
                               .RunStreamingAsync(messages, session, cancellationToken: entry.CancellationTokenSource.Token)
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
            PublishPendingFileChanges(entry, run);
            PublishWorkspaceChanges(entry, run);
            run.MarkCompleted();
            Publish(entry, _eventMapper.CreatePlanUpdated(run, BuildPlan(), ["Executed the task.", "Captured the final workspace state."]));
            Publish(entry, _eventMapper.CreateReasoningSummaryUpdated(run, BuildCompletedSummary(run)));
            Publish(entry, _eventMapper.CreateRunFinished(run, usageTracker.LastTurnUsage, usageTracker.LastTurnCost.HasPrice ? usageTracker.LastTurnCost.TotalCost : null));
            if (!string.IsNullOrWhiteSpace(run.SessionId))
            {
                var assistantMessage = context.AssistantText.Length == 0 ? null : context.AssistantText.ToString();
                var changedFilesCount = entry.FileStateTracker.GetChanges().Count;
                await _sessionService.RecordRunCompletedAsync(run.SessionId, assistantMessage, changedFilesCount, CancellationToken.None).ConfigureAwait(false);
                _conversationAgentManager.MarkTranscriptCount(run.SessionId, sessionDocument.Transcript.Count + (string.IsNullOrWhiteSpace(assistantMessage) ? 1 : 2));
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
                _conversationAgentManager.Invalidate(run.SessionId, CancellationToken.None);
                await _sessionService.RecordRunCancelledAsync(run.SessionId, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent failed while executing run {RunId} in session {SessionId}.", run.Id, run.SessionId);
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
                _conversationAgentManager.Invalidate(run.SessionId, CancellationToken.None);
                await _sessionService.RecordRunFailedAsync(run.SessionId, ex.Message, CancellationToken.None).ConfigureAwait(false);
            }
        }
        finally
        {
            PublishPendingFileChanges(entry, run);
            entry.Complete();
            entry.CancellationTokenSource.Dispose();
        }
    }

    private bool TryGetRun(string sessionId, string runId, out RunStoreEntry entry)
    {
        entry = null!;
        if (string.IsNullOrWhiteSpace(sessionId) || !_runStore.TryGet(runId, out var found) || found is null)
        {
            return false;
        }

        if (!string.Equals(found.Run.SessionId, sessionId, StringComparison.Ordinal))
        {
            return false;
        }

        entry = found;
        return true;
    }

    private static async Task<T> WithSessionInitializationTimeout<T>(Task<T> task, string timeoutMessage, CancellationToken cancellationToken)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(SessionInitializationTimeout, cancellationToken)).ConfigureAwait(false);
        if (completedTask == task)
        {
            return await task.ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new TimeoutException(timeoutMessage);
    }

    private void PublishWorkspaceChanges(RunStoreEntry entry, Run run)
    {
        var changes = entry.FileStateTracker.GetChanges();
        if (changes.Count == 0)
        {
            return;
        }

        var stepId = $"step_{Guid.NewGuid():N}";
        Publish(entry, _eventMapper.CreateStepStarted(run, stepId, "Capture workspace changes"));
        var artifact = _eventMapper.CreateChangesArtifact(changes);
        Publish(entry, _eventMapper.CreateArtifactCreated(run, artifact));
        Publish(entry, _eventMapper.CreateArtifactUpdated(run, artifact));
        Publish(entry, _eventMapper.CreateStepCompleted(run, stepId, "Capture workspace changes"));
    }

    private void PublishExtensions(RunStoreEntry entry, Run run, RunExecutionContext context, EventEnvelope @event)
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

        if (string.Equals(@event.Type, "toolCallResult", StringComparison.Ordinal))
        {
            PublishPendingFileChanges(entry, run);
        }
    }

    private static IEnumerable<ChatMessage> BuildMessages(Run run, IReadOnlyList<SessionTranscriptEntry>? transcript = null)
    {
        if (!string.IsNullOrWhiteSpace(run.WorkingDirectory))
        {
            yield return new ChatMessage(
                ChatRole.System,
                $"Preferred working directory for this task: {run.WorkingDirectory}. Use relative paths from that directory when practical.");
        }

        if (transcript is not null)
        {
            foreach (var entry in transcript)
            {
                yield return new ChatMessage(entry.Role.Trim().ToLowerInvariant() switch
                {
                    "assistant" => ChatRole.Assistant,
                    "system" => ChatRole.System,
                    _ => ChatRole.User,
                }, entry.Text);
            }
        }

        if (!HasTrailingTaskMessage(transcript, run.Task))
        {
            yield return new ChatMessage(ChatRole.User, run.Task);
        }
    }

    private static bool HasTrailingTaskMessage(IReadOnlyList<SessionTranscriptEntry>? transcript, string task)
    {
        var lastMessage = transcript?.LastOrDefault();
        return lastMessage is not null &&
               string.Equals(lastMessage.Role, "user", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(lastMessage.Text, task.Trim(), StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> BuildPlan()
        =>
        [
            "Analyze the task safely.",
            "Execute the agent run and stream events.",
            "Capture workspace changes and final status.",
        ];

    private static ReasoningSummary BuildInitialSummary(Run run)
        => new(
            run.Task,
            BuildPlan(),
            [],
            "Start executing the task.",
            [],
            ["Expose only safe summaries instead of full internal reasoning."]);

    private static ReasoningSummary BuildCompletedSummary(Run run)
        => new(
            run.Task,
            BuildPlan(),
            ["Executed the task.", "Captured the final workspace state."],
            null,
            [],
            ["Reported the final run state through AG-UI events."]);

    private static ReasoningSummary BuildCancelledSummary(Run run)
        => new(
            run.Task,
            BuildPlan(),
            [],
            "Wait for a new run request.",
            ["The run was cancelled before completion."],
            ["Stopped streaming once cancellation was requested."]);

    private static RunDTO MapRun(Run run)
        => new()
        {
            Id = run.Id,
            ThreadId = run.ThreadId,
            SessionId = run.SessionId,
            Task = run.Task,
            WorkingDirectory = run.WorkingDirectory,
            ProviderName = run.ProviderName,
            Model = run.Model,
            Status = run.Status,
            DetailedStatus = run.DetailedStatus,
            CreatedAt = run.CreatedAt,
            UpdatedAt = run.UpdatedAt,
            Error = run.Error,
        };

    private void Publish(RunStoreEntry entry, EventEnvelope @event)
        => entry.Publish(@event);

    private void PublishPendingFileChanges(RunStoreEntry entry, Run run)
    {
        var changes = entry.FileStateTracker.DrainPendingChanges();
        if (changes.Count == 0)
        {
            return;
        }

        foreach (var @event in _codeAgentEventMapper.CreateChangeEvents(run, changes))
        {
            Publish(entry, @event);
        }

        Publish(entry, _codeAgentEventMapper.CreateDiffGenerated(run, changes));
    }

    private string? NormalizeWorkingDirectory(string? workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return null;
        }

        var workspaceRoot = _fileSnapshotService.WorkspaceRoot;
        var candidate = Path.IsPathRooted(workingDirectory)
            ? Path.GetFullPath(workingDirectory)
            : Path.GetFullPath(Path.Combine(workspaceRoot, workingDirectory));

        return candidate;
    }
}
