using EventHorizon.Configuration;
using EventHorizon.Providers;

namespace EventHorizon.EntryPoints;

public interface IRemoteEventHorizonRuntimeFactory
{
    IEventHorizonRuntime Create(AppOptions options);
}
