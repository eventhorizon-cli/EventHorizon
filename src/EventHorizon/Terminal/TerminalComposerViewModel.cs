namespace EventHorizon.Terminal;

public sealed class TerminalComposerViewModel
{
    public string PromptLabel { get; set; } = "❯";
    public string Title { get; set; } = "Prompt composer";
    public string Buffer { get; set; } = string.Empty;
    public int CursorIndex { get; set; }
    public string Hint { get; set; } = "Type a prompt or use /help";
    public string Metadata { get; set; } = string.Empty;
    public bool UseMinimalChrome { get; set; }
}

