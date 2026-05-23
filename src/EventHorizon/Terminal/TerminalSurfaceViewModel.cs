namespace EventHorizon.Terminal;

public sealed class TerminalSurfaceViewModel
{
    public string SurfaceId { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public TerminalPanelViewModel Panel { get; set; } = new();
}

