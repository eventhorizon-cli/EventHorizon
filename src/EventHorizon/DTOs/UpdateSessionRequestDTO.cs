namespace EventHorizon.DTOs;

public sealed class UpdateSessionRequestDTO
{
    public string? Title { get; set; }

    public string? ProviderName { get; set; }

    public string? Model { get; set; }
}
