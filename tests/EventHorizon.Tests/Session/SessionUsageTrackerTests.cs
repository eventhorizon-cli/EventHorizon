using EventHorizon.Engine.Sessions;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using Microsoft.Extensions.AI;

namespace EventHorizon.Tests.Session;

/// <summary>
/// Tests for SessionUsageTracker covering usage tracking, cost calculation, and state management.
/// </summary>
public sealed class SessionUsageTrackerTests
{
    private static readonly ModelPriceCatalog Catalog = new(new Dictionary<string, ModelPriceCatalog.ModelCatalogEntry>
    {
        ["demo-model"] = new()
        {
            InputCostPerToken = 0.001,
            OutputCostPerToken = 0.002,
        },
        ["expensive-model"] = new()
        {
            InputCostPerToken = 0.01,
            OutputCostPerToken = 0.02,
        }
    });

    private static readonly IEventHorizonRuntime FakeRuntime = new FakeRuntimeForTracker();

    [Fact]
    public void Constructor_Initializes_Tracker_With_Catalog_And_Runtime()
    {
        var tracker = new SessionUsageTracker(new ModelPriceCatalogService(Catalog), FakeRuntime);

        Assert.NotNull(tracker.TotalUsage);
        Assert.NotNull(tracker.LastTurnUsage);
    }

    [Fact]
    public void Constructor_Initializes_Tracker_With_Catalog_And_ModelName()
    {
        var tracker = new SessionUsageTracker(new ModelPriceCatalogService(Catalog), "demo-model");

        Assert.NotNull(tracker.TotalUsage);
    }

    [Fact]
    public void StartTurn_Resets_LastTurnUsage()
    {
        var tracker = new SessionUsageTracker(new ModelPriceCatalogService(Catalog), FakeRuntime);

        tracker.StartTurn();

        Assert.NotNull(tracker.LastTurnUsage);
        Assert.Null(tracker.LastTurnUsage.InputTokenCount);
    }

    [Fact]
    public void Reset_Clears_Total_And_Last_Turn_Usage()
    {
        var tracker = new SessionUsageTracker(new ModelPriceCatalogService(Catalog), FakeRuntime);
        tracker.StartTurn();
        tracker.Restore(
            new UsageDetails { InputTokenCount = 12, OutputTokenCount = 8, TotalTokenCount = 20 },
            new UsageCost(12, 8, 0, 0.012m, 0.016m, 0m, 0.028m, true, "USD"));

        tracker.Reset();

        Assert.Equal(0, tracker.TotalUsage.InputTokenCount ?? 0);
        Assert.Equal(0, tracker.TotalUsage.OutputTokenCount ?? 0);
        Assert.Equal(0m, tracker.TotalCost.TotalCost);
        Assert.False(tracker.TotalCost.HasPrice);
        Assert.Equal(0, tracker.LastTurnUsage.InputTokenCount ?? 0);
        Assert.False(tracker.LastTurnCost.HasPrice);
    }

    [Fact]
    public void Restore_Rehydrates_Previous_Total_Usage()
    {
        var tracker = new SessionUsageTracker(new ModelPriceCatalogService(Catalog), FakeRuntime);
        UsageDetails usage = new()
        {
            InputTokenCount = 99,
            OutputTokenCount = 51,
            TotalTokenCount = 150,
        };
        UsageCost cost = new(99, 51, 0, 0.099m, 0.102m, 0m, 0.201m, true, "USD");

        tracker.Restore(usage, cost);

        Assert.Equal(99, tracker.TotalUsage.InputTokenCount);
        Assert.Equal(51, tracker.TotalUsage.OutputTokenCount);
        Assert.Equal(150, tracker.TotalUsage.TotalTokenCount);
        Assert.Equal(0.201m, tracker.TotalCost.TotalCost);
        Assert.True(tracker.TotalCost.HasPrice);
    }

    [Fact]
    public void Restore_Resets_LastTurnUsage_And_Cost()
    {
        var tracker = new SessionUsageTracker(new ModelPriceCatalogService(Catalog), FakeRuntime);
        tracker.StartTurn();

        tracker.Restore(
            new UsageDetails { InputTokenCount = 50, OutputTokenCount = 25 },
            new UsageCost(50, 25, 0, 0.05m, 0.05m, 0m, 0.1m, true, "USD"));

        Assert.Equal(0, tracker.LastTurnUsage.InputTokenCount ?? 0);
        Assert.False(tracker.LastTurnCost.HasPrice);
    }

    [Fact]
    public void StartTurn_Can_Be_Called_Multiple_Times()
    {
        var tracker = new SessionUsageTracker(new ModelPriceCatalogService(Catalog), FakeRuntime);

        tracker.StartTurn();
        var firstLastTurn = tracker.LastTurnUsage;

        tracker.StartTurn();
        var secondLastTurn = tracker.LastTurnUsage;

        Assert.NotNull(firstLastTurn);
        Assert.NotNull(secondLastTurn);
    }

    [Fact]
    public void TotalCost_Initial_State_Is_Unknown()
    {
        var tracker = new SessionUsageTracker(new ModelPriceCatalogService(Catalog), FakeRuntime);

        Assert.False(tracker.TotalCost.HasPrice);
        Assert.Equal(0, tracker.TotalCost.TotalCost);
    }

    [Fact]
    public void LastTurnCost_Initial_State_Is_Unknown()
    {
        var tracker = new SessionUsageTracker(new ModelPriceCatalogService(Catalog), FakeRuntime);

        Assert.False(tracker.LastTurnCost.HasPrice);
        Assert.Equal(0, tracker.LastTurnCost.TotalCost);
    }

    [Fact]
    public void Restore_With_Multiple_Calls_Accumulates_Usage()
    {
        var tracker = new SessionUsageTracker(new ModelPriceCatalogService(Catalog), FakeRuntime);

        tracker.Restore(
            new UsageDetails { InputTokenCount = 50, OutputTokenCount = 25 },
            new UsageCost(50, 25, 0, 0.05m, 0.05m, 0m, 0.1m, true, "USD"));

        Assert.Equal(50, tracker.TotalUsage.InputTokenCount);

        // Second restore should replace (not accumulate)
        tracker.Restore(
            new UsageDetails { InputTokenCount = 30, OutputTokenCount = 20 },
            new UsageCost(30, 20, 0, 0.03m, 0.04m, 0m, 0.07m, true, "USD"));

        Assert.Equal(30, tracker.TotalUsage.InputTokenCount);
    }

    private sealed class FakeRuntimeForTracker : IEventHorizonRuntime
    {
        public ValueTask<string> GetInstructionsAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult("instructions");

        public ValueTask<SessionContextSnapshot> GetContextSnapshotAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new SessionContextSnapshot(
                CurrentDate: "Today's date is 2026-05-21.",
                WorkspaceRoot: "/tmp/workspace",
                WorkspaceSummary: "summary",
                GitStatus: "clean",
                ProjectInstructions: "instructions"));


        public ValueTask<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyList<AITool>>([]);

        public Task InvalidateAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
