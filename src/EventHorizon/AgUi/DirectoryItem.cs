namespace EventHorizon.AGUI;

public sealed record DirectoryItem(
    string Path,
    string Name,
    bool IsDirectory,
    string? ParentPath = null);
