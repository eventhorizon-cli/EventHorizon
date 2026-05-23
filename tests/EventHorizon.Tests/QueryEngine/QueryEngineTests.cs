using EventHorizon.Context;
using EventHorizon.Execution;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Tests.QueryEngine;

public sealed class QueryEngineTests
{
    [Fact]
    public void LoadConversationState_Replaces_History()
    {
        Execution.QueryEngine engine = new(new FakeRuntime(), new SessionUsageTracker(new ModelPriceCatalogService(new ModelPriceCatalog([])), new FakeRuntime()));

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
        public AIAgent Agent => null!;

        public string ModelName => "missing-model";

        public IServiceProvider Services => null!;

        public SessionContextSnapshot ContextSnapshot => new(
            CurrentDate: "Today's date is 2026-05-21.",
            WorkspaceRoot: "/tmp/workspace",
            WorkspaceSummary: "summary",
            GitStatus: "clean",
            ProjectInstructions: "instructions");

        public IReadOnlyList<ToolDescriptor> ToolCatalog => [];

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

