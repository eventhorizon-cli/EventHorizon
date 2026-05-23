using EventHorizon.Configuration;
using EventHorizon.Pricing;

namespace EventHorizon.Terminal.Surfaces;

public sealed class MainSurfaceBuilder : ITerminalSurfaceBuilder
{
    public string SurfaceId => "main";

    public TerminalSurfaceViewModel Build(TerminalSurfaceBuildContext context)
    {
        List<string> lines = [];
        if (context.Transcript.Count == 0)
        {
            lines.AddRange(
            [
                "No transcript yet.",
                "Try prompts like:",
                "- Summarize the repository",
                "- Fix failing tests",
                "- Review the current architecture",
                "- Refactor the TUI into surfaces",
            ]);
        }
        else
        {
            foreach (TerminalMessage entry in context.Transcript)
            {
                var prefix = entry.IsStreamingPreview ? "~" : "•";
                string marker = string.Equals(entry.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                    ? (entry.IsStreamingPreview ? "ASSISTANT · streaming" : "ASSISTANT")
                    : entry.Role.ToUpperInvariant();
                lines.Add($"{prefix} {entry.Timestamp:HH:mm:ss}  {marker}");
                lines.AddRange(entry.Text.Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Split('\n')
                    .Select(static line => string.IsNullOrWhiteSpace(line) ? "  " : $"  {line}"));
                lines.Add(string.Empty);
            }
        }

        return new TerminalSurfaceViewModel
        {
            SurfaceId = SurfaceId,
            IsVisible = true,
            Panel = new TerminalPanelViewModel
            {
                PanelId = TerminalPanelCatalog.Conversation,
                Title = context.IsStreaming ? "Session Thread · live" : "Session Thread",
                IsActive = string.Equals(context.State.ActivePanelId, TerminalPanelCatalog.Conversation, StringComparison.OrdinalIgnoreCase),
                Lines = lines,
                ScrollOffset = context.State.ConversationScrollOffset,
            },
        };
    }
}