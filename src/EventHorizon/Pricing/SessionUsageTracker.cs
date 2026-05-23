using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Pricing;

public sealed class SessionUsageTracker
{
    private readonly ModelPriceCatalog _catalog;
    private readonly string _modelName;

    public SessionUsageTracker(ModelPriceCatalog catalog, string modelName)
    {
        _catalog = catalog;
        _modelName = modelName;
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
        foreach (UsageContent usageContent in update.Contents.OfType<UsageContent>())
        {
            LastTurnUsage.Add(usageContent.Details);
            TotalUsage.Add(usageContent.Details);
        }

        LastTurnCost = _catalog.EstimateCost(_modelName, LastTurnUsage);
        TotalCost = _catalog.EstimateCost(_modelName, TotalUsage);
    }
}

