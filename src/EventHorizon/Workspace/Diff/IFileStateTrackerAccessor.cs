namespace EventHorizon.Workspace.Diff;

public interface IFileStateTrackerAccessor
{
    IFileStateTracker? Current { get; }

    IDisposable BeginScope(IFileStateTracker tracker);
}

