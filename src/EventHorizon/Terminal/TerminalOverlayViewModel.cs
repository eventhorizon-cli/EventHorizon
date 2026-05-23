namespace EventHorizon.Terminal;

public sealed class TerminalOverlayViewModel
{
    public bool IsOpen { get; set; }
    public string OverlayId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public int SelectedIndex { get; set; }
    public IReadOnlyList<string> Lines { get; set; } = [];
}

