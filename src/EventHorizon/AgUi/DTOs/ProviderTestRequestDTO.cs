using EventHorizon.Configuration;

namespace EventHorizon.AGUI.DTOs;

public sealed class ProviderTestRequestDTO
{
    public string Name { get; set; } = string.Empty;

    public ProviderOptions Provider { get; set; } = new();
}

public sealed class ProviderTestResponseDTO
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public IReadOnlyList<string> Models { get; set; } = Array.Empty<string>();
}
