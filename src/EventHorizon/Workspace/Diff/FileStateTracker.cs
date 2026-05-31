namespace EventHorizon.Workspace.Diff;

public sealed class FileStateTracker
{
    private readonly object _gate = new();
    private readonly IFileSnapshotService _fileSnapshotService;
    private readonly IDiffService _diffService;
    private readonly Dictionary<string, TrackedFileState> _states = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _readPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FileChange> _pendingChanges = [];

    public FileStateTracker(string runId, string? sessionId, IFileSnapshotService fileSnapshotService, IDiffService diffService)
    {
        RunId = runId;
        SessionId = sessionId;
        _fileSnapshotService = fileSnapshotService;
        _diffService = diffService;
    }

    public string RunId { get; }

    public string? SessionId { get; }

    public void RecordRead(string path)
    {
        var normalizedPath = _fileSnapshotService.NormalizePath(path);
        lock (_gate)
        {
            _readPaths.Add(normalizedPath);
        }
    }

    public void CaptureBaseline(string path)
    {
        var normalizedPath = _fileSnapshotService.NormalizePath(path);
        var snapshot = _fileSnapshotService.CaptureFile(path);

        lock (_gate)
        {
            var state = GetOrCreateState(normalizedPath);
            EnsureBaseline(state, snapshot);
        }
    }

    public void CaptureCurrent(string path)
    {
        var normalizedPath = _fileSnapshotService.NormalizePath(path);
        var snapshot = _fileSnapshotService.CaptureFile(path);

        lock (_gate)
        {
            var state = GetOrCreateState(normalizedPath);
            EnsureBaseline(state, state.BaselineCaptured ? state.BaselineSnapshot : null);
            state.Path = normalizedPath;
            state.CurrentSnapshot = snapshot;
            QueuePendingNetChange(state);
        }
    }

    public void RecordDelete(string path)
    {
        var normalizedPath = _fileSnapshotService.NormalizePath(path);
        var snapshot = _fileSnapshotService.CaptureFile(path);

        lock (_gate)
        {
            var state = GetOrCreateState(normalizedPath);
            EnsureBaseline(state, snapshot);
            state.CurrentSnapshot = null;
            QueuePendingNetChange(state);
        }
    }

    public void RecordRename(string oldPath, string newPath, FileSnapshot? sourceSnapshotBeforeRename = null)
    {
        var oldNormalizedPath = _fileSnapshotService.NormalizePath(oldPath);
        var newNormalizedPath = _fileSnapshotService.NormalizePath(newPath);
        var newSnapshot = _fileSnapshotService.CaptureFile(newPath);

        lock (_gate)
        {
            var state = FindState(oldNormalizedPath) ?? GetOrCreateState(oldNormalizedPath);
            EnsureBaseline(state, sourceSnapshotBeforeRename ?? state.BaselineSnapshot);
            if (state.BaselineSnapshot is not null)
            {
                state.OldPath ??= oldNormalizedPath;
            }

            _states.Remove(state.Path);
            state.Path = newNormalizedPath;
            state.CurrentSnapshot = newSnapshot;
            _states[newNormalizedPath] = state;

            var renameDiff = _diffService.CreateDiff(newNormalizedPath, oldNormalizedPath, sourceSnapshotBeforeRename, newSnapshot);
            if (renameDiff is not null)
            {
                _pendingChanges.Add(ToChange(renameDiff));
            }
        }
    }

    public void RecordWorkspaceTransition(WorkspaceSnapshot before, WorkspaceSnapshot after)
    {
        var diffs = _diffService.GetDiffs(before, after);

        lock (_gate)
        {
            foreach (var diff in diffs)
            {
                Merge(diff, before, after);
                _pendingChanges.Add(ToChange(diff));
            }
        }
    }

    public IReadOnlyList<FileChange> GetChanges()
    {
        lock (_gate)
        {
            return _states.Values
                .Select(CreateNetDiff)
                .Where(static diff => diff is not null)
                .Select(static diff => ToChange(diff!))
                .OrderBy(static change => change.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public FileDiff? GetDiff(string path)
    {
        var normalizedPath = _fileSnapshotService.NormalizePath(path);
        lock (_gate)
        {
            var state = FindState(normalizedPath);
            return state is null ? null : CreateNetDiff(state);
        }
    }

    public IReadOnlyList<string> GetReadFiles()
    {
        lock (_gate)
        {
            return _readPaths.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    public IReadOnlyList<FileChange> DrainPendingChanges()
    {
        lock (_gate)
        {
            if (_pendingChanges.Count == 0)
            {
                return [];
            }

            var changes = _pendingChanges
                .OrderBy(static change => change.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            _pendingChanges.Clear();
            return changes;
        }
    }

    private void Merge(FileDiff diff, WorkspaceSnapshot before, WorkspaceSnapshot after)
    {
        var lookupPath = diff.OldPath ?? diff.Path;
        var state = FindState(lookupPath) ?? GetOrCreateState(lookupPath);

        switch (diff.Status)
        {
            case "added":
                EnsureBaseline(state, null);
                state.Path = diff.Path;
                state.CurrentSnapshot = after.Entries[diff.Path];
                _states[state.Path] = state;
                break;
            case "deleted":
                EnsureBaseline(state, before.Entries[diff.Path]);
                state.Path = diff.Path;
                state.CurrentSnapshot = null;
                _states[state.Path] = state;
                break;
            case "renamed":
                EnsureBaseline(state, before.Entries[diff.OldPath!]);
                if (state.BaselineSnapshot is not null)
                {
                    state.OldPath ??= diff.OldPath;
                }

                _states.Remove(state.Path);
                state.Path = diff.Path;
                state.CurrentSnapshot = after.Entries[diff.Path];
                _states[state.Path] = state;
                break;
            default:
                EnsureBaseline(state, before.Entries[diff.Path]);
                state.Path = diff.Path;
                state.CurrentSnapshot = after.Entries[diff.Path];
                _states[state.Path] = state;
                break;
        }
    }

    private FileDiff? CreateNetDiff(TrackedFileState state)
    {
        var oldPath = state.BaselineSnapshot is null ? null : state.OldPath;
        return _diffService.CreateDiff(state.Path, oldPath, state.BaselineSnapshot, state.CurrentSnapshot);
    }

    private TrackedFileState GetOrCreateState(string path)
    {
        if (_states.TryGetValue(path, out var state))
        {
            return state;
        }

        state = new TrackedFileState(path);
        _states[path] = state;
        return state;
    }

    private TrackedFileState? FindState(string path)
        => _states.TryGetValue(path, out var state)
            ? state
            : _states.Values.FirstOrDefault(candidate =>
                string.Equals(candidate.Path, path, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.OldPath, path, StringComparison.OrdinalIgnoreCase));

    private static FileChange ToChange(FileDiff diff)
        => new(diff.Path, diff.OldPath, diff.Status, diff.Additions, diff.Deletions, diff.Binary);

    private static void EnsureBaseline(TrackedFileState state, FileSnapshot? snapshot)
    {
        if (state.BaselineCaptured)
        {
            return;
        }

        state.BaselineCaptured = true;
        state.BaselineSnapshot = snapshot;
        state.CurrentSnapshot = snapshot;
    }

    private void QueuePendingNetChange(TrackedFileState state)
    {
        var diff = CreateNetDiff(state);
        if (diff is not null)
        {
            _pendingChanges.Add(ToChange(diff));
        }
    }

    private sealed class TrackedFileState
    {
        public TrackedFileState(string path)
        {
            Path = path;
        }

        public string Path { get; set; }

        public string? OldPath { get; set; }

        public FileSnapshot? BaselineSnapshot { get; set; }

        public FileSnapshot? CurrentSnapshot { get; set; }

        public bool BaselineCaptured { get; set; }
    }
}
