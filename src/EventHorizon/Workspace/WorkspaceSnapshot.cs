namespace EventHorizon.Workspace;

public sealed record WorkspaceSnapshot(
    DateTimeOffset CreatedAt,
    IReadOnlyDictionary<string, WorkspaceSnapshotEntry> Entries);


