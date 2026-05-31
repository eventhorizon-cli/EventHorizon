namespace EventHorizon.Workspace.Diff;

public interface IFileStateTrackerAccessor
{
    FileStateTracker? Current { get; }

    IDisposable BeginScope(FileStateTracker tracker);
}

