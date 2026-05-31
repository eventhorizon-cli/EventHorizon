using EventHorizon.Configuration;
using EventHorizon.DTOs;
using EventHorizon.Engine.Sessions;
using EventHorizon.Providers;
using EventHorizon.Workspace;
using Microsoft.Extensions.AI;

namespace EventHorizon.Tests.Fixtures;

/// <summary>
/// Common test double implementations for interfaces used across tests.
/// </summary>

/// <summary>
/// Records all method calls made to an ISessionStore for verification.
/// </summary>
public sealed class RecordingSessionStore : ISessionStore
{
    public SessionDocument? SavedDocument { get; private set; }
    public List<string> DeletedSessionIds { get; } = [];

    public Task SaveAsync(SessionDocument document, CancellationToken cancellationToken)
    {
        SavedDocument = document;
        return Task.CompletedTask;
    }

    public Task<SessionDocument?> LoadAsync(string sessionId, CancellationToken cancellationToken)
    {
        return Task.FromResult<SessionDocument?>(
            SavedDocument is not null && SavedDocument.Id == sessionId ? SavedDocument : null);
    }

    public Task<IReadOnlyList<SessionSummary>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<SessionSummary>>([]);
    }

    public void Delete(string sessionId, CancellationToken cancellationToken)
    {
        DeletedSessionIds.Add(sessionId);
        if (SavedDocument?.Id == sessionId)
        {
            SavedDocument = null;
        }
    }
}

/// <summary>
/// Returns configurable default values for provider resolution.
/// </summary>
public sealed class StubProviderResolutionService : IProviderResolutionService
{
    private readonly IReadOnlyList<ProviderOptions> _providerOptions;
    private readonly ResolvedProviderContext? _defaultContext;

    public StubProviderResolutionService(
        IReadOnlyList<ProviderOptions>? providerOptions = null,
        ResolvedProviderContext? defaultContext = null)
    {
        _providerOptions = providerOptions ?? [];
        _defaultContext = defaultContext;
    }

    public IReadOnlyList<ProviderOptions> GetProviderOptions() => _providerOptions;

    public ResolvedProviderContext? TryResolveForSession(SessionDocument? session, ChatRequestOverrides? overrides = null)
        => null;

    public ResolvedProviderContext? TryResolveDefault()
        => _defaultContext;
}

/// <summary>
/// Returns a configurable title for testing session title generation.
/// </summary>
public sealed class StubSessionTitleGenerator : ISessionTitleGenerator
{
    private readonly string? _titleToReturn;

    public StubSessionTitleGenerator(string? titleToReturn = null)
    {
        _titleToReturn = titleToReturn;
    }

    public Task<string?> TryGenerateAsync(SessionDocument document, CancellationToken cancellationToken)
        => Task.FromResult(_titleToReturn);
}

/// <summary>
/// Stub implementation for session agent manager.
/// </summary>
public sealed class StubSessionAgentManager : ISessionAgentManager
{
    public Task<SessionAgentRuntime> GetOrCreateAsync(SessionDocument document, ChatRequestOverrides? overrides, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<SessionAgentRuntime> RebuildAsync(SessionDocument document, ChatRequestOverrides? overrides, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public void Invalidate(string sessionId, CancellationToken cancellationToken = default)
    {
    }

    public void InvalidateAll(CancellationToken cancellationToken = default)
    {
    }

    public void MarkTranscriptCount(string sessionId, int transcriptCount)
    {
    }
}

/// <summary>
/// Stub implementation for workspace context accessor.
/// </summary>
public sealed class StubWorkspaceContextAccessor : IWorkspaceContextAccessor
{
    public WorkspaceContext WorkspaceContext { get; set; }

    public StubWorkspaceContextAccessor(string workspaceRoot)
    {
        WorkspaceContext = new WorkspaceContext(workspaceRoot);
    }
}
