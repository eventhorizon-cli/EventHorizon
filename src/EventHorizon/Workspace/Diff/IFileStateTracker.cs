namespace EventHorizon.Workspace.Diff;

public interface IFileStateTracker
{
    string RunId { get; }

    string? SessionId { get; }

    void RecordRead(string path);

    void CaptureBaseline(string path);

    void CaptureCurrent(string path);

    void RecordDelete(string path);

    void RecordRename(string oldPath, string newPath, FileSnapshot? sourceSnapshotBeforeRename = null);

    void RecordWorkspaceTransition(WorkspaceSnapshot before, WorkspaceSnapshot after);

    IReadOnlyList<FileChange> GetChanges();

    FileDiff? GetDiff(string path);

    IReadOnlyList<string> GetReadFiles();

    IReadOnlyList<FileChange> DrainPendingChanges();
}
