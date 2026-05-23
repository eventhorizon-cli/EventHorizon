namespace EventHorizon.Terminal;

public sealed class TerminalPanelViewModel
{
    public string PanelId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IReadOnlyList<string> Lines { get; set; } = [];
    public int ScrollOffset { get; set; }
}

