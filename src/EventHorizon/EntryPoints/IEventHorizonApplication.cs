namespace EventHorizon.EntryPoints;

public interface IEventHorizonApplication
{
    Task<int> RunAsync(CancellationToken cancellationToken);
}
