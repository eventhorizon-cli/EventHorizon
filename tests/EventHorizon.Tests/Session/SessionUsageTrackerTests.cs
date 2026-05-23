using EventHorizon.Pricing;
using Microsoft.Extensions.AI;

namespace EventHorizon.Tests.Session;

public sealed class SessionUsageTrackerTests
{
    private static readonly ModelPriceCatalog Catalog = new(new Dictionary<string, ModelPriceCatalog.ModelCatalogEntry>
    {
        ["demo-model"] = new()
        {
            InputCostPerToken = 0.001,
            OutputCostPerToken = 0.002,
        }
    });

    [Fact]
    public void Reset_Clears_Total_And_Last_Turn_Usage()
    {
        SessionUsageTracker tracker = new(Catalog, "demo-model");
        tracker.StartTurn();
        tracker.Restore(
            new UsageDetails { InputTokenCount = 12, OutputTokenCount = 8, TotalTokenCount = 20 },
            new UsageCost(12, 8, 0, 0.012m, 0.016m, 0m, 0.028m, true, "USD"));

        tracker.Reset();

        Assert.Equal(0, tracker.TotalUsage.InputTokenCount ?? 0);
        Assert.Equal(0, tracker.TotalUsage.OutputTokenCount ?? 0);
        Assert.Equal(0m, tracker.TotalCost.TotalCost);
        Assert.False(tracker.TotalCost.HasPrice);
    }

    [Fact]
    public void Restore_Rehydrates_Previous_Total_Usage()
    {
        SessionUsageTracker tracker = new(Catalog, "demo-model");
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
}

