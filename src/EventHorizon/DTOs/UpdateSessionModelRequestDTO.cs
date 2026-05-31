namespace EventHorizon.DTOs;

public sealed class UpdateSessionModelRequestDTO
{
    public string? ProviderName { get; set; }

    public string? ModelId { get; set; }
}
