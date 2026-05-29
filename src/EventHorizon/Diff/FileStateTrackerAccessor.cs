using System.Threading;

namespace EventHorizon.Diff;

public sealed class FileStateTrackerAccessor
{
    private readonly AsyncLocal<FileStateTracker?> _current = new();

    public FileStateTracker? Current => _current.Value;

    public IDisposable BeginScope(FileStateTracker tracker)
    {
        var previous = _current.Value;
        _current.Value = tracker;
        return new Scope(this, previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly FileStateTrackerAccessor _accessor;
        private readonly FileStateTracker? _previous;
        private bool _disposed;

        public Scope(FileStateTrackerAccessor accessor, FileStateTracker? previous)
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

