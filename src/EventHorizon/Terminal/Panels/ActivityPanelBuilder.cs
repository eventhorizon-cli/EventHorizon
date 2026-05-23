namespace EventHorizon.Terminal.Panels;

public sealed class ActivityPanelBuilder : ITerminalPanelBuilder
{
    public string PanelId => TerminalPanelCatalog.Activity;

    public TerminalPanelViewModel Build(TerminalPanelBuildContext context)
    {
        if (context.State.ActivityFeed.Count == 0)
        {
            return new TerminalPanelViewModel
            {
                PanelId = PanelId,
                Title = "Activity",
                IsActive = string.Equals(context.State.ActivePanelId, PanelId, StringComparison.OrdinalIgnoreCase),
                Lines =
                [
                    "No activity yet.",
                    "Events such as saves, restores, focus changes, and streaming progress appear here."
                ]
            };
        }

        List<string> lines = [];
        foreach (TerminalActivityEntry entry in context.State.ActivityFeed.OrderByDescending(static item => item.Timestamp).Take(10))
        {
            lines.Add($"{entry.Timestamp:HH:mm:ss}  [{entry.Kind}] {entry.Title}");
            if (!string.IsNullOrWhiteSpace(entry.Detail))
            {
                lines.Add($"  {entry.Detail}");
            }
            lines.Add(string.Empty);
        }

        return new TerminalPanelViewModel
        {
            PanelId = PanelId,
            Title = "Activity",
            IsActive = string.Equals(context.State.ActivePanelId, PanelId, StringComparison.OrdinalIgnoreCase),
            Lines = lines
        };
    }
}