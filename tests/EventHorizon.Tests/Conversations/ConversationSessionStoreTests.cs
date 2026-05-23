using EventHorizon.Conversations;

namespace EventHorizon.Tests.Conversations;

public sealed class ConversationSessionStoreTests : IDisposable
{
    private readonly string _root;

    public ConversationSessionStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "eventhorizon-session-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task Save_Load_And_List_Roundtrip_A_Session_Document()
    {
        FileConversationSessionStore store = new(_root);
        ConversationSessionDocument document = new()
        {
            Id = "session-1",
            Name = "demo",
            ProviderType = "openai",
            Model = "gpt-4.1-mini",
            WorkspaceRoot = "/tmp/work",
            Transcript =
            [
                new ConversationTranscriptEntry { Role = "user", Text = "hello" },
                new ConversationTranscriptEntry { Role = "assistant", Text = "hi" }
            ]
        };

        await store.SaveAsync(document, CancellationToken.None);
        ConversationSessionDocument? loaded = await store.LoadAsync("session-1", CancellationToken.None);
        IReadOnlyList<ConversationSessionSummary> list = await store.ListAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("demo", loaded.Name);
        Assert.Equal(2, loaded.Transcript.Count);
        Assert.Single(list);
        Assert.Equal("session-1", list[0].Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

