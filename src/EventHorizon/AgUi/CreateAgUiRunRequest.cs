using System.Text.Json;

namespace EventHorizon.AGUI;

public sealed class CreateAGUIRunRequest
{
    public string? SessionId { get; set; }

    public string Task { get; set; } = string.Empty;

    public string? WorkingDirectory { get; set; }

    public JsonElement? Options { get; set; }
}

