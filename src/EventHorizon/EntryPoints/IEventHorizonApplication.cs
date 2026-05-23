namespace EventHorizon.EntryPoints;

public interface IEventHorizonApplication
{
    Task RunAsync(CancellationToken cancellationToken);
}
