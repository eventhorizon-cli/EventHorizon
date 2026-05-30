using System.Text.Json;

namespace EventHorizon.AGUI.DTOs;

public sealed class CreateAGUIRunRequestDTO
{
    public string? SessionId { get; set; }

    public string Task { get; set; } = string.Empty;

    public string? WorkingDirectory { get; set; }

    public string? ProviderName { get; set; }

    public string? Model { get; set; }

    public JsonElement? Options { get; set; }
}
