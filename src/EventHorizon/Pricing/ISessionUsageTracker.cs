using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Pricing;

public interface ISessionUsageTracker
{
    UsageDetails TotalUsage { get; }
    UsageDetails LastTurnUsage { get; }
    UsageCost TotalCost { get; }
    UsageCost LastTurnCost { get; }
    void StartTurn();
    void Reset();
    void Restore(UsageDetails totalUsage, UsageCost totalCost);
    void ObserveUpdate(AgentResponseUpdate update);
}
