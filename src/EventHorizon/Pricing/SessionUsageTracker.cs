using EventHorizon.Providers;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Pricing;

public sealed class SessionUsageTracker : ISessionUsageTracker
{
    private readonly IModelPriceCatalogService _priceCatalogService;
    private readonly IEventHorizonRuntime _runtime;

    public SessionUsageTracker(
        IModelPriceCatalogService priceCatalogService,
        IEventHorizonRuntime runtime)
    {
        _priceCatalogService = priceCatalogService;
        _runtime = runtime;
    }

    private ModelPriceCatalog? GetCatalog()
    {
        _priceCatalogService.TryGetCatalog(out var catalog);
        return catalog;
    }

    public UsageDetails TotalUsage { get; private set; } = new();

    public UsageDetails LastTurnUsage { get; private set; } = new();

    public UsageCost TotalCost { get; private set; }

    public UsageCost LastTurnCost { get; private set; }

    public void StartTurn()
    {
        LastTurnUsage = new UsageDetails();
        LastTurnCost = UsageCost.Unknown(LastTurnUsage);
    }

    public void Reset()
    {
        TotalUsage = new UsageDetails();
        LastTurnUsage = new UsageDetails();
        TotalCost = UsageCost.Unknown(TotalUsage);
        LastTurnCost = UsageCost.Unknown(LastTurnUsage);
    }

    public void Restore(UsageDetails totalUsage, UsageCost totalCost)
    {
        TotalUsage = new UsageDetails();
        TotalUsage.Add(totalUsage);
        TotalCost = totalCost;
        LastTurnUsage = new UsageDetails();
        LastTurnCost = UsageCost.Unknown(LastTurnUsage);
    }

    public void ObserveUpdate(AgentResponseUpdate update)
    {
        foreach (var usageContent in update.Contents.OfType<UsageContent>())
        {
            LastTurnUsage.Add(usageContent.Details);
            TotalUsage.Add(usageContent.Details);
        }

        var catalog = GetCatalog();
        if (catalog != null)
        {
            LastTurnCost = catalog.EstimateCost(_runtime.ModelName, LastTurnUsage);
            TotalCost = catalog.EstimateCost(_runtime.ModelName, TotalUsage);
        }
    }
}

