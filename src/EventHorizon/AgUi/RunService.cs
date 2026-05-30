using System.Text.Json;
using EventHorizon.AGUI.DTOs;
using EventHorizon.Diff;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Workspace;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace EventHorizon.AGUI;

public sealed class RunService
{
    private static readonly TimeSpan SessionInitializationTimeout = TimeSpan.FromSeconds(30);
    private readonly RunStore _runStore;
    private readonly IEventHorizonRuntime _runtime;
    private readonly IModelPriceCatalogService _priceCatalogService;
    private readonly FileSnapshotService _fileSnapshotService;
    private readonly FileStateTrackerAccessor _fileStateTrackerAccessor;
    private readonly DiffService _diffService;
    private readonly IAGUISessionService _sessionService;
    private readonly AGUIEventMapper _eventMapper;
    private readonly AGUICodeAgentEventMapper _codeAgentEventMapper;
    private readonly IProviderResolutionService _providerResolutionService;
    private readonly IProviderAgentFactory _providerAgentFactory;
    private readonly IConversationAgentManager _conversationAgentManager;
    private readonly ILogger<RunService> _logger;

    public RunService(
        RunStore runStore,
        IEventHorizonRuntime runtime,
        IModelPriceCatalogService priceCatalogService,
        FileSnapshotService fileSnapshotService,
        FileStateTrackerAccessor fileStateTrackerAccessor,
        DiffService diffService,
        IAGUISessionService sessionService,
        AGUIEventMapper eventMapper,
        AGUICodeAgentEventMapper codeAgentEventMapper,
        IProviderResolutionService providerResolutionService,
        IProviderAgentFactory providerAgentFactory,
        IConversationAgentManager conversationAgentManager,
        ILogger<RunService> logger)
    {
        _runStore = runStore;
        _runtime = runtime;
        _priceCatalogService = priceCatalogService;
        _fileSnapshotService = fileSnapshotService;
        _fileStateTrackerAccessor = fileStateTrackerAccessor;
        _diffService = diffService;
        _sessionService = sessionService;
        _eventMapper = eventMapper;
        _codeAgentEventMapper = codeAgentEventMapper;
        _providerResolutionService = providerResolutionService;
        _providerAgentFactory = providerAgentFactory;
        _conversationAgentManager = conversationAgentManager;
        _logger = logger;
    }

    public async Task<AGUIRunDTO> CreateAsync(CreateAGUIRunRequestDTO request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Task))
        {
            throw new ArgumentException("Task is required.", nameof(request));
        }

        // 如果没有提供 workingDirectory 但有 sessionId，从 session 中获取工作目录
        var workingDirectory = request.WorkingDirectory;
        if (string.IsNullOrWhiteSpace(workingDirectory) && !string.IsNullOrWhiteSpace(request.SessionId))
        {
            var sessionDocument = await _sessionService.GetDocumentAsync(request.SessionId, cancellationToken).ConfigureAwait(false);
            if (sessionDocument is not null && !string.IsNullOrWhiteSpace(sessionDocument.WorkspaceRoot))
            {
                workingDirectory = sessionDocument.WorkspaceRoot;
            }
        }

        var normalizedWorkingDirectory = NormalizeWorkingDirectory(workingDirectory);
        var run = new AGUIRun
        {
            Id = $"run_{Guid.NewGuid():N}",
            ThreadId = $"thread_{Guid.NewGuid():N}",
            SessionId = request.SessionId,
            Task = request.Task.Trim(),
            WorkingDirectory = normalizedWorkingDirectory,
            ProviderName = request.ProviderName,
            Model = request.Model,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        run.MarkRunning(AGUIRunStates.Planning);

        if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            var session = await _sessionService.StartRunAsync(request.SessionId, run.Id, run.Task, request.ProviderName, request.Model, cancellationToken).ConfigureAwait(false);
            if (session is null)
            {
                throw new ArgumentException($"Session '{request.SessionId}' was not found.", nameof(request));
            }
        }

        var fileStateTracker = new FileStateTracker(run.Id, run.SessionId, _fileSnapshotService, _diffService);
        var entry = _runStore.Add(new RunStoreEntry(run, fileStateTracker, new CancellationTokenSource()));
        var options = request.Options?.Clone();
        _ = Task.Run(() => ExecuteRunAsync(entry, options), CancellationToken.None);
        return MapRun(run);
    }

    public AGUIRunDTO? Get(string runId)
        => _runStore.TryGet(runId, out var entry) && entry is not null ? MapRun(entry.Run) : null;

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
            _ = _conversationAgentManager.InvalidateAsync(entry.Run.SessionId, CancellationToken.None);
            _ = _sessionService.RecordRunCancelledAsync(entry.Run.SessionId, CancellationToken.None);
        }

        return true;
    }

    public IAsyncEnumerable<AGUIEventEnvelope>? StreamEventsAsync(string runId, long? afterSequence, CancellationToken cancellationToken)
        => _runStore.TryGet(runId, out var entry) && entry is not null
            ? entry.SubscribeAsync(afterSequence, cancellationToken)
            : null;

    public Task<IReadOnlyList<FileChange>?> GetChangesAsync(string runId, CancellationToken cancellationToken)
    {
        if (!_runStore.TryGet(runId, out var entry) || entry is null)
        {
            return Task.FromResult<IReadOnlyList<FileChange>?>(null);
        }

        return Task.FromResult<IReadOnlyList<FileChange>?>(entry.FileStateTracker.GetChanges());
    }

    public Task<FileDiff?> GetDiffAsync(string runId, string path, CancellationToken cancellationToken)
    {
        if (!_runStore.TryGet(runId, out var entry) || entry is null)
        {
            return Task.FromResult<FileDiff?>(null);
        }

        return Task.FromResult(entry.FileStateTracker.GetDiff(path));
    }

    private async Task ExecuteRunAsync(RunStoreEntry entry, JsonElement? options)
    {
        var run = entry.Run;
        var context = new AGUIRunExecutionContext();
        var usageTracker = default(SessionUsageTracker);

        try
        {
            var sessionDocument = string.IsNullOrWhiteSpace(run.SessionId)
                ? null
                : await _sessionService.GetDocumentAsync(run.SessionId, CancellationToken.None).ConfigureAwait(false);
            var overrides = new ChatRequestOverrides
            {
                ProviderName = run.ProviderName,
                Model = run.Model,
            };
            var resolvedProvider = _providerResolutionService.TryResolveForSession(sessionDocument, overrides);

            if (resolvedProvider is null)
            {
                throw new InvalidOperationException("No provider is configured. Please open settings and configure a provider before sending messages.");
            }

            ConversationAgentRuntime? conversationRuntime = null;
            AIAgent agent;
            AgentSession? session = null;

            if (sessionDocument is not null && !string.IsNullOrWhiteSpace(run.SessionId))
            {
                conversationRuntime = await WithSessionInitializationTimeout(
                    _conversationAgentManager.GetOrCreateAsync(sessionDocument, overrides, entry.CancellationTokenSource.Token),
                    "Timed out while initializing the conversation agent.",
                    entry.CancellationTokenSource.Token).ConfigureAwait(false);
                agent = conversationRuntime.Agent;
                session = conversationRuntime.Session;
            }
            else
            {
                var runtimeOptions = new Configuration.AppOptions
                {
                    AGUI = new Configuration.AGUIOptions
                    {
                        ApiBasePath = string.Empty,
                        RawEndpointPath = string.Empty,
                        Urls = new(),
                    },
                    Agent = new Configuration.AgentOptions
                    {
                        Name = _runtime.Agent.Name ?? string.Empty,
                        Description = _runtime.Agent.Description ?? string.Empty,
                        EnableSkills = true,
                        EnableShell = true,
                        EnableMcpTools = true,
                        AdditionalSystemPrompts = [],
                    },
                    Provider = resolvedProvider.Provider,
                    CurrentDefaultProvider = resolvedProvider.ProviderName,
                    Providers = new Dictionary<string, Configuration.ProviderOptions>(StringComparer.OrdinalIgnoreCase),
                    Pricing = new Configuration.PricingOptions(),
                    Conversation = new Configuration.ConversationOptions(),
                    McpServers = [],
                };

                agent = _providerAgentFactory.CreateAgent(
                    runtimeOptions,
                    _runtime.Instructions,
                    _runtime.Tools,
                    _runtime.SkillsProvider,
                    _runtime.Services);
            }

            var usageRuntime = new EventHorizonRuntime(
                agent,
                _runtime.Services,
                resolvedProvider.Model,
                _runtime.Instructions,
                _runtime.ContextSnapshot,
                _runtime.ToolCatalog,
                _runtime.Tools,
                _runtime.SkillsProvider,
                []);
            usageTracker = new SessionUsageTracker(_priceCatalogService, usageRuntime);

            Publish(entry, _eventMapper.CreateRunStarted(run, resolvedProvider.Model, run.WorkingDirectory, options));
            Publish(entry, _eventMapper.CreateUserMessage(run));
            Publish(entry, _eventMapper.CreatePlanUpdated(run, BuildPlan(), []));
            Publish(entry, _eventMapper.CreateReasoningSummaryUpdated(run, BuildInitialSummary(run)));
            Publish(entry, _eventMapper.CreateStepStarted(run, context.ExecutionStepId, "Plan and execute task"));

            run.MarkRunning(AGUIRunStates.Executing);
            using var fileTrackingScope = _fileStateTrackerAccessor.BeginScope(entry.FileStateTracker);
            session ??= await WithSessionInitializationTimeout(
                agent.CreateSessionAsync(cancellationToken: entry.CancellationTokenSource.Token).AsTask(),
                "Timed out while creating the provider session.",
                entry.CancellationTokenSource.Token).ConfigureAwait(false);
            usageTracker.StartTurn();

            var messages = conversationRuntime is not null && !conversationRuntime.WasReused
                ? BuildMessages(run, sessionDocument?.Transcript)
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
                _conversationAgentManager.MarkTranscriptCount(run.SessionId, sessionDocument?.Transcript.Count + (string.IsNullOrWhiteSpace(assistantMessage) ? 1 : 2) ?? 0);
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
                await _conversationAgentManager.InvalidateAsync(run.SessionId, CancellationToken.None).ConfigureAwait(false);
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
                await _conversationAgentManager.InvalidateAsync(run.SessionId, CancellationToken.None).ConfigureAwait(false);
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

    private void PublishWorkspaceChanges(RunStoreEntry entry, AGUIRun run)
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

        if (string.Equals(@event.Type, "toolCallResult", StringComparison.Ordinal))
        {
            PublishPendingFileChanges(entry, run);
        }
    }

    private static IEnumerable<ChatMessage> BuildMessages(AGUIRun run, IReadOnlyList<Conversations.ConversationTranscriptEntry>? transcript = null)
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

    private static AGUIRunDTO MapRun(AGUIRun run)
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

    private void Publish(RunStoreEntry entry, AGUIEventEnvelope @event)
        => entry.Publish(@event);

    private void PublishPendingFileChanges(RunStoreEntry entry, AGUIRun run)
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
