namespace EventHorizon.DTOs;

public sealed record DirectoryItemDTO(
    string Path,
    string Name,
    bool IsDirectory,
    string? ParentPath = null);
