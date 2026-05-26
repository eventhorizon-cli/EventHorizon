namespace EventHorizon.Terminal.Models;

public sealed class TerminalContextFile
{
    public string Path { get; set; } = string.Empty;

    public bool IsSelected { get; set; }

    public string? Description { get; set; }

    public long? SizeBytes { get; set; }
}

