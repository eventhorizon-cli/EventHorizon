namespace EventHorizon.Workspace.Diff;

public interface IFileSnapshotService
{
    string WorkspaceRoot { get; }

    FileSnapshot? CaptureFile(string path);

    WorkspaceSnapshot CaptureWorkspace();

    string NormalizePath(string path);

    string ResolvePath(string path);
}

