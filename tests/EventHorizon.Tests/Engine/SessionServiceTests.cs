using EventHorizon.Configuration;
using EventHorizon.DTOs;
using EventHorizon.Engine;
using EventHorizon.Engine.Sessions;
using EventHorizon.Providers;
using EventHorizon.Workspace;

namespace EventHorizon.Tests.Engine;

public sealed class SessionServiceTests
{
    [Fact]
    public async Task CreateAsync_Updates_WorkspaceRoot_When_Request_Selects_Directory()
    {
        using var workspace = new TemporaryWorkspace();
        var selectedRoot = Path.Combine(workspace.Root, "selected");
        Directory.CreateDirectory(selectedRoot);

        var workspaceContext = new WorkspaceContext(workspace.Root);
        var store = new RecordingSessionStore();
        var service = new SessionService(
            store,
            workspaceContext,
            new StubProviderResolutionService(),
            new StubSessionTitleGenerator(),
            new StubSessionAgentManager());

        var result = await service.CreateAsync(new CreateSessionRequestDTO
        {
            WorkspaceRoot = selectedRoot,
        }, CancellationToken.None);

        Assert.Equal(Path.GetFullPath(selectedRoot), workspaceContext.WorkspaceRoot);
        Assert.NotNull(store.SavedDocument);
        Assert.Equal(Path.GetFullPath(selectedRoot), store.SavedDocument!.WorkspaceRoot);
        Assert.Equal(Path.GetFullPath(selectedRoot), result.WorkspaceRoot);
    }

    private sealed class RecordingSessionStore : ISessionStore
    {
        public SessionDocument? SavedDocument { get; private set; }

        public Task SaveAsync(SessionDocument document, CancellationToken cancellationToken)
        {
            SavedDocument = document;
            return Task.CompletedTask;
        }

        public Task<SessionDocument?> LoadAsync(string sessionId, CancellationToken cancellationToken)
            => Task.FromResult<SessionDocument?>(SavedDocument is not null && SavedDocument.Id == sessionId ? SavedDocument : null);

        public Task<IReadOnlyList<SessionSummary>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SessionSummary>>([]);

        public void Delete(string sessionId, CancellationToken cancellationToken)
        {
            if (SavedDocument?.Id == sessionId)
            {
                SavedDocument = null;
            }
        }
    }

    private sealed class StubProviderResolutionService : IProviderResolutionService
    {
        public IReadOnlyList<ProviderOptions> GetProviderOptions() => [];

        public ResolvedProviderContext? TryResolveForSession(SessionDocument? session, ChatRequestOverrides? overrides = null)
            => null;

        public ResolvedProviderContext? TryResolveDefault()
            => null;
    }

    private sealed class StubSessionTitleGenerator : ISessionTitleGenerator
    {
        public Task<string?> TryGenerateAsync(SessionDocument document, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);
    }

    private sealed class StubSessionAgentManager : ISessionAgentManager
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

    private sealed class TemporaryWorkspace : IDisposable
    {
        public TemporaryWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "eventhorizon-session-service-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
