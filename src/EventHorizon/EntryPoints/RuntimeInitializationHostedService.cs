using Microsoft.Extensions.Hosting;

namespace EventHorizon.EntryPoints;

internal sealed class RuntimeInitializationHostedService : IHostedService
{
    private readonly IEventHorizonRuntimeInitializer _runtimeInitializer;

    public RuntimeInitializationHostedService(IEventHorizonRuntimeInitializer runtimeInitializer)
    {
        _runtimeInitializer = runtimeInitializer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => _runtimeInitializer.InitializeAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
