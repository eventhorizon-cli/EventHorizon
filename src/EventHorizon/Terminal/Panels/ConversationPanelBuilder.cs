namespace EventHorizon.Terminal.Panels;

public sealed class ConversationPanelBuilder : ITerminalPanelBuilder
{
    public string PanelId => TerminalPanelCatalog.Conversation;

    public TerminalPanelViewModel Build(TerminalPanelBuildContext context)
    {
        if (context.Transcript.Count == 0)
        {
            return new TerminalPanelViewModel
            {
                PanelId = PanelId,
                Title = "Conversation",
                IsActive = string.Equals(context.State.ActivePanelId, PanelId, StringComparison.OrdinalIgnoreCase),
                Lines =
                [
                    "No transcript yet.",
                    "Start with a prompt like:",
                    "- Summarize this repository",
                    "- Fix failing tests",
                    "- Review the current architecture"
                ]
            };
        }

        List<string> lines = [];
        foreach (TerminalMessage entry in context.Transcript)
        {
            var prefix = entry.IsStreamingPreview ? "~" : "•";
            lines.Add($"{prefix} {entry.Timestamp:HH:mm:ss}  {entry.Role.ToUpperInvariant()}");
            lines.AddRange(entry.Text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Select(static line => string.IsNullOrWhiteSpace(line) ? "  " : $"  {line}"));
            lines.Add(string.Empty);
        }

        return new TerminalPanelViewModel
        {
            PanelId = PanelId,
            Title = "Conversation",
            IsActive = string.Equals(context.State.ActivePanelId, PanelId, StringComparison.OrdinalIgnoreCase),
            Lines = lines,
            ScrollOffset = context.State.ConversationScrollOffset
        };
    }
}
