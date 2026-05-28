namespace EventHorizon.Workspace;

public sealed record FileChange(
    string Path,
    string? OldPath,
    string Status,
    int Additions,
    int Deletions,
    bool Binary);


