using EventHorizon.Engine.Sessions;

namespace EventHorizon.Tests.Fixtures;

/// <summary>
/// Builder for creating test SessionDocument instances with fluent API.
/// </summary>
public sealed class SessionDocumentBuilder
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "session";
    private string _status = "idle";
    private string? _providerName;
    private string _providerType = "openai";
    private string _model = "gpt-4o-mini";
    private string _workspaceRoot = Path.GetTempPath();
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;
    private string? _lastRunId;
    private string? _summary;
    private int _changedFilesCount = 0;
    private bool _isTitleGenerated = false;
    private bool _isTitleManuallyEdited = false;
    private string? _serializedSession;
    private string? _sessionId;
    private List<SessionTranscriptEntry> _transcript = [];

    public SessionDocumentBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public SessionDocumentBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public SessionDocumentBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public SessionDocumentBuilder WithProviderType(string providerType)
    {
        _providerType = providerType;
        return this;
    }

    public SessionDocumentBuilder WithProviderName(string? providerName)
    {
        _providerName = providerName;
        return this;
    }

    public SessionDocumentBuilder WithModel(string model)
    {
        _model = model;
        return this;
    }

    public SessionDocumentBuilder WithWorkspaceRoot(string workspaceRoot)
    {
        _workspaceRoot = workspaceRoot;
        return this;
    }

    public SessionDocumentBuilder WithCreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public SessionDocumentBuilder WithUpdatedAt(DateTimeOffset updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public SessionDocumentBuilder WithTranscript(params SessionTranscriptEntry[] entries)
    {
        _transcript = new List<SessionTranscriptEntry>(entries);
        return this;
    }

    public SessionDocumentBuilder WithSummary(string? summary)
    {
        _summary = summary;
        return this;
    }

    public SessionDocumentBuilder WithLastRunId(string? lastRunId)
    {
        _lastRunId = lastRunId;
        return this;
    }

    public SessionDocumentBuilder WithSerializedSession(string? serializedSession)
    {
        _serializedSession = serializedSession;
        return this;
    }

    public SessionDocumentBuilder WithSessionId(string? sessionId)
    {
        _sessionId = sessionId;
        return this;
    }

    public SessionDocumentBuilder WithChangedFilesCount(int count)
    {
        _changedFilesCount = count;
        return this;
    }

    public SessionDocument Build()
    {
        return new SessionDocument
        {
            Id = _id,
            Name = _name,
            Status = _status,
            ProviderName = _providerName,
            ProviderType = _providerType,
            Model = _model,
            WorkspaceRoot = _workspaceRoot,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            LastRunId = _lastRunId,
            Summary = _summary,
            ChangedFilesCount = _changedFilesCount,
            IsTitleGenerated = _isTitleGenerated,
            IsTitleManuallyEdited = _isTitleManuallyEdited,
            SerializedSession = _serializedSession,
            SessionId = _sessionId,
            Transcript = _transcript,
        };
    }
}
