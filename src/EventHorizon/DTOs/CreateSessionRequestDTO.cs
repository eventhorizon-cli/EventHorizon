namespace EventHorizon.DTOs;

public sealed class CreateSessionRequestDTO
{
    public string? InitialMessage { get; set; }

    public string? ProviderName { get; set; }

    public string? Model { get; set; }

    public string? WorkspaceRoot { get; set; }
}
