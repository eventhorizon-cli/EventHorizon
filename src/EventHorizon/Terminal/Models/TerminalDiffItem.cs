namespace EventHorizon.Terminal.Models;

public enum TerminalDiffKind
{
    Added,
    Modified,
    Deleted,
    Renamed,
}

public sealed class TerminalDiffItem
{
    public string Path { get; set; } = string.Empty;

    public TerminalDiffKind Kind { get; set; }

    public string? Summary { get; set; }

    public string? UnifiedDiff { get; set; }
}

