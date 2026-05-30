namespace EventHorizon.EntryPoints;

public interface IEventHorizonRuntimeInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
