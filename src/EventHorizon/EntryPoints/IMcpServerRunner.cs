using EventHorizon.Providers;

namespace EventHorizon.EntryPoints;

public interface IMcpServerRunner
{
    Task RunAsync(IEventHorizonRuntime runtime, CancellationToken cancellationToken);
}
