using EventHorizon.Execution;
using EventHorizon.Pricing;
using EventHorizon.Providers;

namespace EventHorizon.EntryPoints.Console;

public interface IQueryEngineFactory
{
    QueryEngine Create(IEventHorizonRuntime runtime, SessionUsageTracker usageTracker);
}
