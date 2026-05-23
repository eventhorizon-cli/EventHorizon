using EventHorizon.Execution;
using EventHorizon.Providers;
using EventHorizon.Pricing;

namespace EventHorizon.EntryPoints.Console;

public sealed class QueryEngineFactory : IQueryEngineFactory
{
    public QueryEngine Create(IEventHorizonRuntime runtime, SessionUsageTracker usageTracker) => new(runtime, usageTracker);
}
