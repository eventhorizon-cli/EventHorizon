using EventHorizon.Providers;

namespace EventHorizon.EntryPoints;

internal sealed class EventHorizonRuntimeHolder
{
    public IEventHorizonRuntime? Runtime { get; set; }
}
