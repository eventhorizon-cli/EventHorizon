using System.Threading;

namespace EventHorizon.Workspace.Diff;

public sealed class FileStateTrackerAccessor : IFileStateTrackerAccessor
{
    private readonly AsyncLocal<IFileStateTracker?> _current = new();

    public IFileStateTracker? Current => _current.Value;

    public IDisposable BeginScope(IFileStateTracker tracker)
    {
        var previous = _current.Value;
        _current.Value = tracker;
        return new Scope(this, previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly FileStateTrackerAccessor _accessor;
        private readonly IFileStateTracker? _previous;
        private bool _disposed;

        public Scope(FileStateTrackerAccessor accessor, IFileStateTracker? previous)
        {
            _accessor = accessor;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _accessor._current.Value = _previous;
            _disposed = true;
        }
    }
}
