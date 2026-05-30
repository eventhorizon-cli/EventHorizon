using EventHorizon.AGUI;
using EventHorizon.Configuration;
using EventHorizon.Conversations;
using EventHorizon.Providers;

namespace EventHorizon.Tests.AGUI;

public sealed class ConversationModelServiceTests
{
    [Fact]
    public async Task UpdateAsync_Persists_New_Model_And_Rebuilds_Current_Conversation_Only()
    {
        var store = new InMemoryConversationSessionStore();
        var first = new ConversationSessionDocument
        {
            Id = "conversation-1",
            Name = "First",
            ProviderName = "openai",
            ProviderType = "openai",
            Model = "gpt-4.1-mini",
            Transcript =
            [
                new ConversationTranscriptEntry { Role = "user", Text = "hello" },
                new ConversationTranscriptEntry { Role = "assistant", Text = "hi" },
            ],
        };
        var second = new ConversationSessionDocument
        {
            Id = "conversation-2",
            Name = "Second",
            ProviderName = "anthropic",
            ProviderType = "anthropic",
            Model = "claude-sonnet-4-20250514",
        };
        await store.SaveAsync(first, CancellationToken.None);
        await store.SaveAsync(second, CancellationToken.None);

        var agentManager = new RecordingConversationAgentManager();
        var service = new ConversationModelService(store, new FakeProviderResolutionService(), agentManager);

        var result = await service.UpdateAsync("conversation-1", "openai", "gpt-4.1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("conversation-1", result.ConversationId);
        Assert.Equal("openai", result.ProviderName);
        Assert.Equal("gpt-4.1", result.ModelId);

        var updated = await store.LoadAsync("conversation-1", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("gpt-4.1", updated.Model);
        Assert.Equal(2, updated.Transcript.Count);
        Assert.Equal("hello", updated.Transcript[0].Text);

        var untouched = await store.LoadAsync("conversation-2", CancellationToken.None);
        Assert.NotNull(untouched);
        Assert.Equal("claude-sonnet-4-20250514", untouched.Model);

        Assert.Equal(["conversation-1"], agentManager.RebuiltConversationIds);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Friendly_Error_When_Provider_Is_Missing()
    {
        var store = new InMemoryConversationSessionStore();
        await store.SaveAsync(new ConversationSessionDocument
        {
            Id = "conversation-1",
            Name = "First",
            ProviderName = "openai",
            ProviderType = "openai",
            Model = "gpt-4.1-mini",
        }, CancellationToken.None);

        var service = new ConversationModelService(
            store,
            new NullProviderResolutionService(),
            new RecordingConversationAgentManager());

        var error = await Assert.ThrowsAsync<ConversationModelUpdateException>(() =>
            service.UpdateAsync("conversation-1", "missing", "gpt-4.1", CancellationToken.None));

        Assert.Contains("missing", error.Message, StringComparison.Ordinal);
    }

    private sealed class InMemoryConversationSessionStore : IConversationSessionStore
    {
        private readonly Dictionary<string, ConversationSessionDocument> _documents = new(StringComparer.Ordinal);

        public Task SaveAsync(ConversationSessionDocument document, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _documents[document.Id] = Clone(document);
            return Task.CompletedTask;
        }

        public Task<ConversationSessionDocument?> LoadAsync(string sessionId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_documents.TryGetValue(sessionId, out var document) ? Clone(document) : null);
        }

        public Task<IReadOnlyList<ConversationSessionSummary>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ConversationSessionSummary>>([]);

        public Task DeleteAsync(string sessionId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _documents.Remove(sessionId);
            return Task.CompletedTask;
        }

        private static ConversationSessionDocument Clone(ConversationSessionDocument document)
            => new()
            {
                Id = document.Id,
                Name = document.Name,
                Status = document.Status,
                ProviderName = document.ProviderName,
                ProviderType = document.ProviderType,
                Model = document.Model,
                WorkspaceRoot = document.WorkspaceRoot,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt,
                LastRunId = document.LastRunId,
                Summary = document.Summary,
                ChangedFilesCount = document.ChangedFilesCount,
                IsTitleGenerated = document.IsTitleGenerated,
                IsTitleManuallyEdited = document.IsTitleManuallyEdited,
                SerializedSession = document.SerializedSession,
                ConversationId = document.ConversationId,
                Usage = document.Usage,
                Transcript = document.Transcript
                    .Select(static entry => new ConversationTranscriptEntry
                    {
                        Role = entry.Role,
                        Text = entry.Text,
                        Timestamp = entry.Timestamp,
                    })
                    .ToList(),
            };
    }

    private sealed class FakeProviderResolutionService : IProviderResolutionService
    {
        public IReadOnlyList<ProviderOptions> GetProviderOptions()
            => [];

        public ResolvedProviderContext? TryResolveForSession(ConversationSessionDocument? session, ChatRequestOverrides? overrides = null)
        {
            var providerName = overrides?.ProviderName ?? session?.ProviderName ?? "openai";
            var model = overrides?.Model ?? session?.Model ?? "gpt-4.1-mini";
            return new ResolvedProviderContext(
                providerName,
                providerName,
                model,
                new ProviderOptions
                {
                    Name = providerName,
                    Type = providerName,
                    Model = model,
                    Models = [model],
                },
                overrides ?? ChatRequestOverrides.Empty);
        }

        public ResolvedProviderContext? TryResolveDefault()
            => TryResolveForSession(null, ChatRequestOverrides.Empty);
    }

    private sealed class NullProviderResolutionService : IProviderResolutionService
    {
        public IReadOnlyList<ProviderOptions> GetProviderOptions()
            => [];

        public ResolvedProviderContext? TryResolveForSession(ConversationSessionDocument? session, ChatRequestOverrides? overrides = null)
            => null;

        public ResolvedProviderContext? TryResolveDefault()
            => null;
    }

    private sealed class RecordingConversationAgentManager : IConversationAgentManager
    {
        public List<string> RebuiltConversationIds { get; } = [];

        public Task<ConversationAgentRuntime> GetOrCreateAsync(ConversationSessionDocument document, ChatRequestOverrides? overrides, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ConversationAgentRuntime> RebuildAsync(ConversationSessionDocument document, ChatRequestOverrides? overrides, CancellationToken cancellationToken)
        {
            RebuiltConversationIds.Add(document.Id);
            return Task.FromResult<ConversationAgentRuntime>(null!);
        }

        public Task InvalidateAsync(string sessionId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task InvalidateAllAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void MarkTranscriptCount(string sessionId, int transcriptCount)
        {
        }
    }
}
