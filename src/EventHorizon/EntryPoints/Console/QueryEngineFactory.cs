using EventHorizon.Execution;
using EventHorizon.Pricing;
using EventHorizon.Providers;

namespace EventHorizon.EntryPoints.Console;

public sealed class QueryEngineFactory : IQueryEngineFactory
{
    public QueryEngine Create(IEventHorizonRuntime runtime, SessionUsageTracker usageTracker) => new(runtime, usageTracker);
}
