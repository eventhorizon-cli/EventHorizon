namespace EventHorizon.AGUI.DTOs;

public sealed record DirectoryListingDTO(
    string CurrentPath,
    IReadOnlyList<DirectoryItemDTO> Items);
