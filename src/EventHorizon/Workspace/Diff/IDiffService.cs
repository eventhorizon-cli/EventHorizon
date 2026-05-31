namespace EventHorizon.Workspace.Diff;

public interface IDiffService
{
    IReadOnlyList<FileChange> GetChanges(WorkspaceSnapshot before, WorkspaceSnapshot after);

    FileDiff? GetDiff(WorkspaceSnapshot before, WorkspaceSnapshot after, string path);

    IReadOnlyList<FileDiff> GetDiffs(WorkspaceSnapshot before, WorkspaceSnapshot after);

    FileDiff? CreateDiff(string path, string? oldPath, FileSnapshot? oldSnapshot, FileSnapshot? newSnapshot);
}

