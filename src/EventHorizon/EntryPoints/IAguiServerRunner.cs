using EventHorizon.Configuration;
using EventHorizon.Providers;

namespace EventHorizon.EntryPoints;

public interface IAguiServerRunner
{
    Task RunAsync(AppOptions options, IEventHorizonRuntime runtime, CancellationToken cancellationToken);
}
