namespace EventHorizon.EntryPoints;

public interface IAGUIServerRunner
{
    Task RunAsync(CancellationToken cancellationToken);
}
