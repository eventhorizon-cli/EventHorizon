using EventHorizon.Configuration;
using EventHorizon.Context;
using EventHorizon.Conversations;
using EventHorizon.Execution;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventHorizon.Tests.QueryEngine;

public sealed class QueryEngineTests
{
    [Fact]
    public void LoadConversationState_Replaces_History()
    {
        Execution.QueryEngine engine = new(
            new FakeRuntime(),
            new FakeProviderAgentFactory(),
            new FakeSkillProviderFactory(),
            new FakeOptionsMonitor<AppOptions>(new AppOptions()),
            new ServiceCollection().BuildServiceProvider(),
            new SessionUsageTracker(new ModelPriceCatalogService(new ModelPriceCatalog([])), "missing-model"));

        engine.LoadConversationState(
            session: null,
            history:
            [
                new ConversationEntry(ChatRole.User, "inspect src/"),
                new ConversationEntry(ChatRole.Assistant, "Here is what I found.")
            ]);

        Assert.Collection(
            engine.History,
            entry =>
            {
                Assert.Equal(ChatRole.User, entry.Role);
                Assert.Equal("inspect src/", entry.Text);
            },
            entry =>
            {
                Assert.Equal(ChatRole.Assistant, entry.Role);
                Assert.Equal("Here is what I found.", entry.Text);
            });
    }

    private sealed class FakeRuntime : IEventHorizonRuntime
    {
        public string ModelName => "missing-model";
        public ValueTask<string> GetInstructionsAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("instructions");
        public ValueTask<SessionContextSnapshot> GetContextSnapshotAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new SessionContextSnapshot(
                CurrentDate: "Today's date is 2026-05-21.",
                WorkspaceRoot: "/tmp/workspace",
                WorkspaceSummary: "summary",
                GitStatus: "clean",
                ProjectInstructions: "instructions"));
        public ValueTask<IReadOnlyList<ToolDescriptor>> GetToolCatalogAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlyList<ToolDescriptor>>([]);
        public ValueTask<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlyList<AITool>>([]);
        public Task InvalidateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeProviderAgentFactory : IProviderAgentFactory
    {
        public AIAgent CreateAgent(AppOptions options, string instructions, IReadOnlyList<AITool> tools, AgentSkillsProvider? skillsProvider, IServiceProvider services) => null!;
    }

    private sealed class FakeSkillProviderFactory : ISkillProviderFactory
    {
        public AgentSkillsProvider? Create(AppOptions options, IServiceProvider services, ConversationSessionDocument? document = null) => null;
    }

    private sealed class FakeOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
