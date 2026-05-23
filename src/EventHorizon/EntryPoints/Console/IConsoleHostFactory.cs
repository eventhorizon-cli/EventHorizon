using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Terminal;

namespace EventHorizon.EntryPoints.Console;

public interface IConsoleHostFactory
{
    ConsoleHost Create(IEventHorizonRuntime runtime, ModelPriceCatalog catalog);
}
