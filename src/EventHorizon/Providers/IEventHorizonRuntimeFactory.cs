namespace EventHorizon.Providers;

public interface IEventHorizonRuntimeFactory
{
    Task<IEventHorizonRuntime> CreateAsync(Configuration.AppOptions options, CancellationToken cancellationToken);
}

