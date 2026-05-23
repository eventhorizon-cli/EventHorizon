namespace EventHorizon.Terminal;

public sealed class TerminalNavigationItemViewModel
{
    public string PanelId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Shortcut { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

