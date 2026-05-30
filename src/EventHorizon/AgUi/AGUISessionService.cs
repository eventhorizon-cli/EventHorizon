using EventHorizon.AGUI.DTOs;
using EventHorizon.Configuration;
using EventHorizon.Conversations;
using EventHorizon.Providers;
using EventHorizon.Workspace;
using Microsoft.Extensions.Options;

namespace EventHorizon.AGUI;

public interface IAGUISessionService
{
    Task<IReadOnlyList<AGUISessionSummaryDTO>> ListAsync(CancellationToken cancellationToken);

    Task<AGUISessionDetailDTO?> GetAsync(string sessionId, CancellationToken cancellationToken);

    Task<ConversationSessionDocument?> GetDocumentAsync(string sessionId, CancellationToken cancellationToken);

    Task<AGUISessionSummaryDTO> CreateAsync(CreateAGUISessionRequestDTO request, CancellationToken cancellationToken);

    Task<AGUISessionSummaryDTO?> UpdateAsync(string sessionId, UpdateAGUISessionRequestDTO request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken);

    Task<ConversationSessionDocument?> StartRunAsync(
        string sessionId,
        string runId,
        string task,
        string? providerName,
        string? model,
        CancellationToken cancellationToken);

    Task RecordRunCompletedAsync(string sessionId, string? assistantMessage, int changedFilesCount, CancellationToken cancellationToken);

    Task RecordRunFailedAsync(string sessionId, string error, CancellationToken cancellationToken);

    Task RecordRunCancelledAsync(string sessionId, CancellationToken cancellationToken);

    Task GenerateTitleIfNeededAsync(string sessionId, CancellationToken cancellationToken);
}

public sealed class AGUISessionService : IAGUISessionService
{
    private const int MaxSummaryLength = 240;
    private readonly IConversationSessionStore _conversationSessionStore;
    private readonly AppOptions _options;
    private readonly WorkspaceContext _workspaceContext;
    private readonly IProviderResolutionService _providerResolutionService;
    private readonly ISessionTitleGenerator _sessionTitleGenerator;
    private readonly IConversationAgentManager _conversationAgentManager;

    public AGUISessionService(
        IConversationSessionStore conversationSessionStore,
        IOptions<AppOptions> options,
        WorkspaceContext workspaceContext,
        IProviderResolutionService providerResolutionService,
        ISessionTitleGenerator sessionTitleGenerator,
        IConversationAgentManager conversationAgentManager)
    {
        _conversationSessionStore = conversationSessionStore;
        _options = options.Value;
        _workspaceContext = workspaceContext;
        _providerResolutionService = providerResolutionService;
        _sessionTitleGenerator = sessionTitleGenerator;
        _conversationAgentManager = conversationAgentManager;
    }

    public async Task<IReadOnlyList<AGUISessionSummaryDTO>> ListAsync(CancellationToken cancellationToken)
        => (await _conversationSessionStore.ListAsync(cancellationToken).ConfigureAwait(false))
            .Select(MapSummary)
            .ToArray();

    public async Task<AGUISessionDetailDTO?> GetAsync(string sessionId, CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return document is null ? null : MapDetail(document);
    }

    public Task<ConversationSessionDocument?> GetDocumentAsync(string sessionId, CancellationToken cancellationToken)
        => _conversationSessionStore.LoadAsync(sessionId, cancellationToken);

    public async Task<AGUISessionSummaryDTO> CreateAsync(CreateAGUISessionRequestDTO request, CancellationToken cancellationToken)
    {
        var initialMessage = request.InitialMessage?.Trim();
        var workspaceRoot = string.IsNullOrWhiteSpace(request.WorkspaceRoot) ? _workspaceContext.WorkspaceRoot : Path.GetFullPath(request.WorkspaceRoot);
        var document = CreateSessionDocument(initialMessage, workspaceRoot, request.ProviderName, request.Model);
        await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        return MapSummary(document);
    }

    public async Task<AGUISessionSummaryDTO?> UpdateAsync(string sessionId, UpdateAGUISessionRequestDTO request, CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
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
        await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        await _conversationAgentManager.InvalidateAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return MapSummary(document);
    }

    public async Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return false;
        }

        await _conversationSessionStore.DeleteAsync(sessionId, cancellationToken).ConfigureAwait(false);
        await _conversationAgentManager.InvalidateAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<ConversationSessionDocument?> StartRunAsync(
        string sessionId,
        string runId,
        string task,
        string? providerName,
        string? model,
        CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        document.LastRunId = runId;
        document.Status = AGUIRunStates.Running;
        if (providerName is not null)
        {
            document.ProviderName = string.IsNullOrWhiteSpace(providerName) ? null : providerName.Trim();
        }

        if (model is not null)
        {
            document.Model = string.IsNullOrWhiteSpace(model) ? string.Empty : model.Trim();
        }

        document.UpdatedAt = DateTimeOffset.UtcNow;
        AppendUserMessageIfNeeded(document, task);
        ApplyResolvedProvider(document);
        await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        return document;
    }

    public async Task RecordRunCompletedAsync(
        string sessionId,
        string? assistantMessage,
        int changedFilesCount,
        CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return;
        }

        document.Status = AGUIRunStates.Completed;
        document.ChangedFilesCount = changedFilesCount;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(assistantMessage))
        {
            document.Transcript.Add(new ConversationTranscriptEntry
            {
                Role = "assistant",
                Text = assistantMessage.Trim(),
                Timestamp = DateTimeOffset.UtcNow,
            });
            document.Summary = Truncate(assistantMessage.Trim(), MaxSummaryLength);
        }

        await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        await GenerateTitleIfNeededAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordRunFailedAsync(string sessionId, string error, CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return;
        }

        document.Status = AGUIRunStates.Failed;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        document.Summary = Truncate(error, MaxSummaryLength);
        await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordRunCancelledAsync(string sessionId, CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return;
        }

        document.Status = AGUIRunStates.Cancelled;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
    }

    public async Task GenerateTitleIfNeededAsync(string sessionId, CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
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
        await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
    }

    private ConversationSessionDocument CreateSessionDocument(string? initialMessage, string workspaceRoot, string? providerName, string? model)
    {
        var now = DateTimeOffset.UtcNow;
        var resolved = _providerResolutionService.TryResolveForSession(
            null,
            new ChatRequestOverrides
            {
                ProviderName = providerName,
                Model = model,
            });

        var document = new ConversationSessionDocument
        {
            Name = BuildInitialTitle(initialMessage),
            Status = AGUIRunStates.Idle,
            ProviderName = resolved?.ProviderName ?? providerName,
            ProviderType = resolved?.ProviderType ?? _options.Provider.Type ?? string.Empty,
            Model = resolved?.Model ?? model ?? _options.Provider.Model ?? string.Empty,
            WorkspaceRoot = workspaceRoot,
            CreatedAt = now,
            UpdatedAt = now,
            IsTitleGenerated = false,
            IsTitleManuallyEdited = false,
        };

        if (!string.IsNullOrWhiteSpace(initialMessage))
        {
            document.Transcript.Add(new ConversationTranscriptEntry
            {
                Role = "user",
                Text = initialMessage,
                Timestamp = now,
            });
        }

        return document;
    }

    private void ApplyResolvedProvider(ConversationSessionDocument document)
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

    private static void AppendUserMessageIfNeeded(ConversationSessionDocument document, string task)
    {
        var normalizedTask = task.Trim();
        var lastMessage = document.Transcript.LastOrDefault();
        if (lastMessage is not null &&
            string.Equals(lastMessage.Role, "user", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(lastMessage.Text, normalizedTask, StringComparison.Ordinal))
        {
            return;
        }

        document.Transcript.Add(new ConversationTranscriptEntry
        {
            Role = "user",
            Text = normalizedTask,
            Timestamp = DateTimeOffset.UtcNow,
        });
    }

    private static AGUISessionSummaryDTO MapSummary(ConversationSessionSummary summary)
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
            summary.WorkspaceRoot);

    private static AGUISessionSummaryDTO MapSummary(ConversationSessionDocument document)
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
            document.WorkspaceRoot);

    private static AGUISessionDetailDTO MapDetail(ConversationSessionDocument document)
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
            document.WorkspaceRoot,
            document.SessionSkills,
            document.Transcript
                .Select((entry, index) => new AGUIChatMessageDTO(
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

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength].TrimEnd() + "…";
}
