namespace EventHorizon.DTOs;

public sealed class CreateDirectoryRequestDTO
{
    public string? ParentPath { get; set; }

    public string? Name { get; set; }
}

