using EventHorizon.Pricing;
using EventHorizon.Terminal.Session;

namespace EventHorizon.EntryPoints.Console;

public interface ISessionUsageTrackerFactory
{
    SessionUsageTracker Create(ModelPriceCatalog catalog, string modelName);
}
