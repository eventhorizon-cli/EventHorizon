using EventHorizon.Configuration;
using EventHorizon.Engine.Sessions;

namespace EventHorizon.Tests.Sessions;

public sealed class SessionStoreTests : IDisposable
{
    private readonly string _root;
    private readonly string _homeDirectory;

    public SessionStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "eventhorizon-session-tests", Guid.NewGuid().ToString("N"));
        _homeDirectory = Path.Combine(_root, "home");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_homeDirectory);
    }

    [Fact]
    public async Task Save_Load_And_List_Roundtrip_A_Session_Document()
    {
        FileSessionStore store = new(new TestPathEnvironment(_root, _homeDirectory));
        WorkspaceDocument workspace = new()
        {
            Id = "workspace-1",
            Name = "work",
            WorkspaceRoot = "/tmp/work",
            SessionIds = ["session-1"],
        };
        SessionDocument document = new()
        {
            Id = "session-1",
            WorkspaceId = workspace.Id,
            Name = "demo",
            ProviderType = "openai",
            Model = "gpt-4.1-mini",
            WorkspaceRoot = "/tmp/work",
            Transcript =
            [
                new SessionTranscriptEntry { Role = "user", Text = "hello" },
                new SessionTranscriptEntry { Role = "assistant", Text = "hi" }
            ]
        };

        await store.SaveWorkspaceAsync(workspace, CancellationToken.None);
        await store.SaveAsync(document, CancellationToken.None);
        var loaded = await store.LoadAsync("session-1", CancellationToken.None);
        var list = await store.ListAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("demo", loaded.Name);
        Assert.Equal(2, loaded.Transcript.Count);
        Assert.Single(list);
        Assert.Equal("session-1", list[0].Id);
    }

    [Fact]
    public async Task Store_Uses_Home_Directory_Session_Subdirectory()
    {
        FileSessionStore store = new(new TestPathEnvironment(_root, _homeDirectory));
        WorkspaceDocument workspace = new()
        {
            Id = "workspace-2",
            Name = "work",
            WorkspaceRoot = _root,
            SessionIds = ["session-2"],
        };
        SessionDocument document = new()
        {
            Id = "session-2",
            WorkspaceId = workspace.Id,
            WorkspaceRoot = _root,
            Transcript = [new SessionTranscriptEntry { Role = "user", Text = "hello" }]
        };

        await store.SaveWorkspaceAsync(workspace, CancellationToken.None);
        await store.SaveAsync(document, CancellationToken.None);

        var sessionDirectory = Path.Combine(_homeDirectory, ".eventhorizon", "workspaces", "workspace-2", "session-2");
        var sessionFile = Path.Combine(sessionDirectory, "session.json");
        Assert.True(Directory.Exists(sessionDirectory));
        Assert.True(File.Exists(sessionFile));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class TestPathEnvironment : IPathEnvironment
    {
        public TestPathEnvironment(string currentDirectory, string homeDirectory)
        {
            CurrentDirectory = currentDirectory;
            HomeDirectory = homeDirectory;
        }

        public string CurrentDirectory { get; }

        public string HomeDirectory { get; }
    }
}
