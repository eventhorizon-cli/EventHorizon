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
            // Use minimal/Opencode-like prefix: empty for final replies, a subtle symbol for streaming.
            var prefix = entry.IsStreamingPreview ? "⟳" : " ";
            // Map certain roles to clearer display labels in the conversation thread.
            var roleLabel = entry.Role switch
            {
                var r when string.Equals(r, "tool", StringComparison.OrdinalIgnoreCase) => "TOOL CALL",
                var r when string.Equals(r, "tool-result", StringComparison.OrdinalIgnoreCase) => "TOOL RESULT",
                var r when string.Equals(r, "thought", StringComparison.OrdinalIgnoreCase) => "THOUGHT",
                _ => entry.Role.ToUpperInvariant(),
            };
            // Show role header without timestamp. For assistant streaming show marker so renderer can color appropriately.
            var headerMarker = entry.IsStreamingPreview && string.Equals(entry.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                ? $"{roleLabel} · streaming"
                : roleLabel;

            lines.Add($"{prefix}  {headerMarker}");
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
