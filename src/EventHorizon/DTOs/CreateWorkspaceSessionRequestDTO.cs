namespace EventHorizon.DTOs;

public sealed class CreateWorkspaceSessionRequestDTO
{
    public string? InitialMessage { get; set; }

    public string? ProviderName { get; set; }

    public string? Model { get; set; }
}
