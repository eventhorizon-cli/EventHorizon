using System.Text.Json;

namespace EventHorizon.DTOs;

public sealed class CreateRunRequestDTO
{
    public string? SessionId { get; set; }

    public string Task { get; set; } = string.Empty;


    public JsonElement? Options { get; set; }
}
