namespace EventHorizon.Diff;

public sealed record WorkspaceSnapshot(
    DateTimeOffset CreatedAt,
    IReadOnlyDictionary<string, FileSnapshot> Entries);

