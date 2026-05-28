using EventHorizon.Configuration;
using EventHorizon.Conversations;
using EventHorizon.Workspace;

namespace EventHorizon.AGUI;

public sealed class AGUISessionService
{
    private const int MaxSummaryLength = 240;
    private readonly IConversationSessionStore _conversationSessionStore;
    private readonly AppOptions _options;
    private readonly WorkspaceContext _workspaceContext;

    public AGUISessionService(
        IConversationSessionStore conversationSessionStore,
        AppOptions options,
        WorkspaceContext workspaceContext)
    {
        _conversationSessionStore = conversationSessionStore;
        _options = options;
        _workspaceContext = workspaceContext;
    }

    public async Task<IReadOnlyList<AGUISessionSummary>> ListAsync(CancellationToken cancellationToken)
        => (await _conversationSessionStore.ListAsync(cancellationToken).ConfigureAwait(false))
            .Select(MapSummary)
            .ToArray();

    public async Task<AGUISessionDetail?> GetAsync(string sessionId, CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return document is null ? null : MapDetail(document);
    }

    public Task<ConversationSessionDocument?> GetDocumentAsync(string sessionId, CancellationToken cancellationToken)
        => _conversationSessionStore.LoadAsync(sessionId, cancellationToken);

    public async Task<AGUISessionSummary> CreateAsync(CreateAGUISessionRequest request, CancellationToken cancellationToken)
    {
        var initialMessage = request.InitialMessage?.Trim();
        var document = CreateSessionDocument(initialMessage);
        await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        return MapSummary(document);
    }

    public async Task<AGUISessionSummary?> UpdateTitleAsync(string sessionId, string title, CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        document.Name = string.IsNullOrWhiteSpace(title) ? document.Name : title.Trim();
        document.IsTitleGenerated = true;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
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
        return true;
    }

    public async Task<ConversationSessionDocument?> StartRunAsync(string sessionId, string runId, string task, CancellationToken cancellationToken)
    {
        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        document.LastRunId = runId;
        document.Status = AGUIRunStates.Running;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        AppendUserMessageIfNeeded(document, task);
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

    private ConversationSessionDocument CreateSessionDocument(string? initialMessage)
    {
        var now = DateTimeOffset.UtcNow;
        var document = new ConversationSessionDocument
        {
            Name = BuildInitialTitle(initialMessage),
            Status = AGUIRunStates.Idle,
            ProviderType = _options.Provider.Type ?? string.Empty,
            Model = _options.Provider.Model ?? string.Empty,
            WorkspaceRoot = _workspaceContext.WorkspaceRoot,
            CreatedAt = now,
            UpdatedAt = now,
            IsTitleGenerated = false,
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

    private static AGUISessionSummary MapSummary(ConversationSessionSummary summary)
        => new(
            summary.Id,
            summary.Name,
            summary.Status,
            summary.CreatedAt,
            summary.UpdatedAt,
            summary.LastRunId,
            summary.Summary,
            summary.ChangedFilesCount,
            summary.IsTitleGenerated);

    private static AGUISessionSummary MapSummary(ConversationSessionDocument document)
        => new(
            document.Id,
            document.Name,
            document.Status,
            document.CreatedAt,
            document.UpdatedAt,
            document.LastRunId,
            document.Summary,
            document.ChangedFilesCount,
            document.IsTitleGenerated);

    private static AGUISessionDetail MapDetail(ConversationSessionDocument document)
        => new(
            document.Id,
            document.Name,
            document.Status,
            document.CreatedAt,
            document.UpdatedAt,
            document.LastRunId,
            document.Summary,
            document.ChangedFilesCount,
            document.IsTitleGenerated,
            document.Transcript
                .Select((entry, index) => new AGUIChatMessage(
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

