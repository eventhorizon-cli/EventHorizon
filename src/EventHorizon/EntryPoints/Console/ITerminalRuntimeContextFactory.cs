using EventHorizon.Configuration;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Terminal;

namespace EventHorizon.EntryPoints.Console;

public interface ITerminalRuntimeContextFactory
{
    TerminalRuntimeContext Create(IEventHorizonRuntime runtime, AppOptions options, SessionUsageTracker usageTracker);
}
