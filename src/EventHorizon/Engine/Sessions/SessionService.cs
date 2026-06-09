using EventHorizon.Configuration;
using EventHorizon.DTOs;
using EventHorizon.Engine.Runs;
using EventHorizon.Providers;
using EventHorizon.Workspace;

namespace EventHorizon.Engine.Sessions;

public sealed class SessionService : ISessionService
{
    private const int MaxSummaryLength = 240;
    private readonly ISessionStore _sessionStore;
    private readonly IWorkspaceContextAccessor _workspaceContextAccessor;
    private readonly IProviderResolutionService _providerResolutionService;
    private readonly ISessionTitleGenerator _sessionTitleGenerator;
    private readonly ISessionAgentManager _agentManager;

    public SessionService(
        ISessionStore sessionStore,
        IWorkspaceContextAccessor workspaceContextAccessor,
        IProviderResolutionService providerResolutionService,
        ISessionTitleGenerator sessionTitleGenerator,
        ISessionAgentManager agentManager)
    {
        _sessionStore = sessionStore;
        _workspaceContextAccessor = workspaceContextAccessor;
        _providerResolutionService = providerResolutionService;
        _sessionTitleGenerator = sessionTitleGenerator;
        _agentManager = agentManager;
    }

    public async Task<IReadOnlyList<SessionSummaryDTO>> ListAsync(CancellationToken cancellationToken)
        => (await _sessionStore.ListAsync(cancellationToken).ConfigureAwait(false))
            .Select(MapSummary)
            .ToArray();

    public async Task<SessionDetailDTO?> GetAsync(string sessionId, CancellationToken cancellationToken)
    {
        var document = await _sessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        var workspace = string.IsNullOrWhiteSpace(document.WorkspaceId)
            ? null
            : await _sessionStore.LoadWorkspaceAsync(document.WorkspaceId, cancellationToken).ConfigureAwait(false);
        if (workspace is null)
        {
            return null;
        }

        return MapDetail(document, workspace);
    }

    public Task<SessionDocument?> GetDocumentAsync(string sessionId, CancellationToken cancellationToken)
        => _sessionStore.LoadAsync(sessionId, cancellationToken);

    public async Task<SessionSummaryDTO> CreateAsync(CreateSessionRequestDTO request, CancellationToken cancellationToken)
    {
        var initialMessage = request.InitialMessage?.Trim();
        var workspace = await ResolveOrCreateWorkspaceAsync(request.WorkspaceId, request.WorkspaceRoot, cancellationToken).ConfigureAwait(false);
        var document = CreateSessionDocument(initialMessage, workspace, request.ProviderName, request.Model);
        workspace.SessionIds.RemoveAll(id => string.Equals(id, document.Id, StringComparison.OrdinalIgnoreCase));
        workspace.SessionIds.Add(document.Id);
        workspace.UpdatedAt = document.UpdatedAt;
        await _sessionStore.SaveWorkspaceAsync(workspace, cancellationToken).ConfigureAwait(false);
        await _sessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        return MapSummary(document, workspace);
    }

    public async Task<SessionSummaryDTO?> UpdateAsync(string sessionId, UpdateSessionRequestDTO request, CancellationToken cancellationToken)
    {
        var document = await _sessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            document.Name = request.Title.Trim();
            document.IsTitleGenerated = false;
            document.IsTitleManuallyEdited = true;
        }

        if (request.ProviderName is not null)
        {
            document.ProviderName = string.IsNullOrWhiteSpace(request.ProviderName) ? null : request.ProviderName.Trim();
        }

        if (request.Model is not null)
        {
            document.Model = string.IsNullOrWhiteSpace(request.Model) ? string.Empty : request.Model.Trim();
        }

        ApplyResolvedProvider(document);
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _sessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        _agentManager.Invalidate(sessionId, cancellationToken);
        return MapSummary(document);
    }

    public async Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken)
    {
        var document = await _sessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return false;
        }

        _sessionStore.Delete(sessionId, cancellationToken);
        _agentManager.Invalidate(sessionId, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteWorkspaceAsync(string workspaceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            return false;
        }

        var workspace = await _sessionStore.LoadWorkspaceAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        if (workspace is null)
        {
            return false;
        }

        foreach (var sessionId in workspace.SessionIds)
        {
            _agentManager.Invalidate(sessionId, cancellationToken);
        }

        _sessionStore.DeleteWorkspace(workspace.Id, cancellationToken);
        return true;
    }

    public async Task<SessionDocument?> StartRunAsync(
        string sessionId,
        string runId,
        string task,
        CancellationToken cancellationToken)
    {
        var document = await _sessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        document.LastRunId = runId;
        document.Status = RunStates.Running;

        document.UpdatedAt = DateTimeOffset.UtcNow;
        AppendUserMessageIfNeeded(document, task);
        ApplyResolvedProvider(document);
        await _sessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        return document;
    }

    public async Task RecordRunCompletedAsync(
        string sessionId,
        string? assistantMessage,
        int changedFilesCount,
        CancellationToken cancellationToken)
    {
        var document = await _sessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return;
        }

        document.Status = RunStates.Completed;
        document.ChangedFilesCount = changedFilesCount;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(assistantMessage))
        {
            document.Transcript.Add(new SessionTranscriptEntry
            {
                Role = "assistant",
                Text = assistantMessage.Trim(),
                Timestamp = DateTimeOffset.UtcNow,
            });
            document.Summary = Truncate(assistantMessage.Trim(), MaxSummaryLength);
        }

        await _sessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        await GenerateTitleIfNeededAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordRunFailedAsync(string sessionId, string error, CancellationToken cancellationToken)
    {
        var document = await _sessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return;
        }

        document.Status = RunStates.Failed;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        document.Summary = Truncate(error, MaxSummaryLength);
        await _sessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordRunCancelledAsync(string sessionId, CancellationToken cancellationToken)
    {
        var document = await _sessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return;
        }

        document.Status = RunStates.Cancelled;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _sessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
    }

    public async Task GenerateTitleIfNeededAsync(string sessionId, CancellationToken cancellationToken)
    {
        var document = await _sessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null || document.IsTitleManuallyEdited || document.Transcript.Count < 2)
        {
            return;
        }

        var generatedTitle = await _sessionTitleGenerator.TryGenerateAsync(document, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(generatedTitle) || string.Equals(generatedTitle, document.Name, StringComparison.Ordinal))
        {
            return;
        }

        document.Name = generatedTitle;
        document.IsTitleGenerated = true;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _sessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
    }

    private SessionDocument CreateSessionDocument(string? initialMessage, WorkspaceDocument workspace, string? providerName, string? model)
    {
        var now = DateTimeOffset.UtcNow;
        var document = new SessionDocument
        {
            WorkspaceId = workspace.Id,
            Name = BuildInitialTitle(initialMessage),
            Status = RunStates.Idle,
            ProviderName = string.IsNullOrWhiteSpace(providerName) ? null : providerName.Trim(),
            Model = string.IsNullOrWhiteSpace(model) ? string.Empty : model.Trim(),
            WorkspaceRoot = workspace.WorkspaceRoot,
            CreatedAt = now,
            UpdatedAt = now,
            IsTitleGenerated = false,
            IsTitleManuallyEdited = false,
        };

        var resolved = _providerResolutionService.TryResolveForSession(document);
        var activeProvider = resolved?.Provider ?? new ProviderOptions();
        document.ProviderName = resolved?.ProviderName ?? document.ProviderName;
        document.ProviderType = resolved?.ProviderType ?? activeProvider.Type ?? string.Empty;
        document.Model = resolved?.Model ?? document.Model ?? activeProvider.DefaultModel ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(initialMessage))
        {
            document.Transcript.Add(new SessionTranscriptEntry
            {
                Role = "user",
                Text = initialMessage,
                Timestamp = now,
            });
        }

        return document;
    }

    private async Task<WorkspaceDocument> ResolveOrCreateWorkspaceAsync(string? workspaceId, string? requestedWorkspaceRoot, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(workspaceId))
        {
            var existing = await _sessionStore.LoadWorkspaceAsync(workspaceId.Trim(), cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                Directory.CreateDirectory(existing.WorkspaceRoot);
                return existing;
            }
        }

        var workspaceRoot = ResolveWorkspaceRoot(requestedWorkspaceRoot);
        Directory.CreateDirectory(workspaceRoot);
        var now = DateTimeOffset.UtcNow;
        return new WorkspaceDocument
        {
            Name = BuildWorkspaceName(workspaceRoot),
            WorkspaceRoot = workspaceRoot,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private string ResolveWorkspaceRoot(string? requestedWorkspaceRoot)
    {
        var baseWorkspaceRoot = _workspaceContextAccessor.WorkspaceContext.WorkspaceRoot;
        if (string.IsNullOrWhiteSpace(requestedWorkspaceRoot))
        {
            return baseWorkspaceRoot;
        }

        return Path.GetFullPath(Path.Combine(baseWorkspaceRoot, requestedWorkspaceRoot));
    }

    private void ApplyResolvedProvider(SessionDocument document)
    {
        var resolved = _providerResolutionService.TryResolveForSession(document);
        if (resolved is null)
        {
            return;
        }

        document.ProviderName = resolved.ProviderName;
        document.ProviderType = resolved.ProviderType;
        document.Model = resolved.Model;
    }

    private static void AppendUserMessageIfNeeded(SessionDocument document, string task)
    {
        var normalizedTask = task.Trim();
        var lastMessage = document.Transcript.LastOrDefault();
        if (lastMessage is not null &&
            string.Equals(lastMessage.Role, "user", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(lastMessage.Text, normalizedTask, StringComparison.Ordinal))
        {
            return;
        }

        document.Transcript.Add(new SessionTranscriptEntry
        {
            Role = "user",
            Text = normalizedTask,
            Timestamp = DateTimeOffset.UtcNow,
        });
    }

    private static SessionSummaryDTO MapSummary(SessionSummary summary)
        => new(
            summary.Id,
            summary.Name,
            summary.Status,
            summary.CreatedAt,
            summary.UpdatedAt,
            summary.ProviderName,
            summary.ProviderType,
            summary.Model,
            summary.LastRunId,
            summary.Summary,
            summary.ChangedFilesCount,
            summary.IsTitleGenerated,
            summary.WorkspaceId,
            summary.WorkspaceName,
            summary.WorkspaceRoot);

    private static SessionSummaryDTO MapSummary(SessionDocument document)
        => MapSummary(document, workspace: null);

    private static SessionSummaryDTO MapSummary(SessionDocument document, WorkspaceDocument? workspace)
        => new(
            document.Id,
            document.Name,
            document.Status,
            document.CreatedAt,
            document.UpdatedAt,
            document.ProviderName,
            document.ProviderType,
            document.Model,
            document.LastRunId,
            document.Summary,
            document.ChangedFilesCount,
            document.IsTitleGenerated,
            document.WorkspaceId,
            workspace?.Name,
            workspace?.WorkspaceRoot);

    private static SessionDetailDTO MapDetail(SessionDocument document, WorkspaceDocument? workspace)
        => new(
            document.Id,
            document.Name,
            document.Status,
            document.CreatedAt,
            document.UpdatedAt,
            document.ProviderName,
            document.ProviderType,
            document.Model,
            document.LastRunId,
            document.Summary,
            document.ChangedFilesCount,
            document.IsTitleGenerated,
            document.WorkspaceId,
            workspace?.Name,
            workspace?.WorkspaceRoot,
            workspace?.WorkspaceSkills ?? new SkillsOptions(),
            document.Transcript
                .Select((entry, index) => new ChatMessageDTO(
                    $"msg_{document.Id}_{index + 1}",
                    document.Id,
                    NormalizeRole(entry.Role),
                    entry.Text,
                    entry.Timestamp,
                    "completed"))
                .ToArray());

    private static string NormalizeRole(string role)
        => role.Trim().ToLowerInvariant() switch
        {
            "assistant" => "assistant",
            "system" => "system",
            _ => "user",
        };

    private static string BuildInitialTitle(string? message)
    {
        var normalized = string.Join(' ', (message ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "New conversation";
        }

        return Truncate(normalized, 60);
    }

    private static string BuildWorkspaceName(string workspaceRoot)
    {
        var name = Path.GetFileName(workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(name) ? "Workspace" : name;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength].TrimEnd() + "…";
}
