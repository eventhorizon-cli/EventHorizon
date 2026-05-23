using EventHorizon.Pricing;
using EventHorizon.Terminal.Session;

namespace EventHorizon.EntryPoints.Console;

public sealed class SessionUsageTrackerFactory : ISessionUsageTrackerFactory
{
    public SessionUsageTracker Create(ModelPriceCatalog catalog, string modelName) => new(catalog, modelName);
}
