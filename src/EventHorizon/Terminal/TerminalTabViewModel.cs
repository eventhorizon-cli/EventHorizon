namespace EventHorizon.Terminal;

public sealed class TerminalTabViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

