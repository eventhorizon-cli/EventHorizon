using EventHorizon.Configuration;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Terminal;

namespace EventHorizon.EntryPoints.Console;

public interface ITerminalWorkbenchHostFactory
{
    TerminalWorkbenchHost Create(IEventHorizonRuntime runtime, AppOptions options, ModelPriceCatalog catalog);
}
