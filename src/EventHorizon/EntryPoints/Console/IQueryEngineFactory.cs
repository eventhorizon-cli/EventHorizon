using EventHorizon.Execution;
using EventHorizon.Providers;
using EventHorizon.Pricing;

namespace EventHorizon.EntryPoints.Console;

public interface IQueryEngineFactory
{
    QueryEngine Create(IEventHorizonRuntime runtime, SessionUsageTracker usageTracker);
}
