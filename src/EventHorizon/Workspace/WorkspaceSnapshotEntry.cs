namespace EventHorizon.Workspace;

public sealed record WorkspaceSnapshotEntry(
    string Path,
    bool Binary,
    string Hash,
    string? Text);


