using System.Text.Json;

namespace EventHorizon.DTOs;

public sealed class CreateRunRequestDTO
{
    public string? SessionId { get; set; }

    public string Task { get; set; } = string.Empty;

    public string? WorkingDirectory { get; set; }

    public string? ProviderName { get; set; }

    public string? Model { get; set; }

    public JsonElement? Options { get; set; }
}
