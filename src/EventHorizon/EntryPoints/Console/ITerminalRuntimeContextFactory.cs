using EventHorizon.Configuration;
using EventHorizon.Providers;
using EventHorizon.Terminal;
using EventHorizon.Pricing;

namespace EventHorizon.EntryPoints.Console;

public interface ITerminalRuntimeContextFactory
{
    TerminalRuntimeContext Create(IEventHorizonRuntime runtime, AppOptions options, SessionUsageTracker usageTracker);
}
