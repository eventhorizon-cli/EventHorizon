namespace EventHorizon.Workspace;

public sealed record FileDiff(
    string Path,
    string? OldPath,
    string Status,
    string? OldText,
    string? NewText,
    string Language,
    bool Binary,
    int Additions,
    int Deletions);


