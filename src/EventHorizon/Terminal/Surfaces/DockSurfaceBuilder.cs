using EventHorizon.Configuration;
using EventHorizon.Pricing;

namespace EventHorizon.Terminal.Surfaces;

public sealed class DockSurfaceBuilder : ITerminalSurfaceBuilder
{
    public string SurfaceId => "dock";

    public TerminalSurfaceViewModel Build(TerminalSurfaceBuildContext context)
    {
        List<string> lines =
        [
            context.IsStreaming ? "Live stream" : "Ready state",
            context.IsStreaming
                ? $"Assistant preview length: {context.State.LastAssistantPreview.Length} chars"
                : $"Sidebar mode: {TerminalCommandCatalog.GetSidebarModeLabel(context.State.SidebarMode)}",
            string.Empty,
        ];

        List<TerminalActivityEntry> recent = context.State.ActivityFeed.OrderByDescending(static entry => entry.Timestamp).Take(6).ToList();
        if (recent.Count == 0)
        {
            lines.Add("No recent activity.");
        }
        else
        {
            foreach (TerminalActivityEntry entry in recent)
            {
                lines.Add($"{entry.Timestamp:HH:mm:ss}  [{entry.Kind}] {entry.Title}");
                if (!string.IsNullOrWhiteSpace(entry.Detail))
                {
                    lines.Add($"  {entry.Detail}");
                }
            }
        }

        return new TerminalSurfaceViewModel
        {
            SurfaceId = SurfaceId,
            IsVisible = true,
            Panel = new TerminalPanelViewModel
            {
                PanelId = "dock",
                Title = context.IsStreaming ? "Tool Activity Dock" : "Command + Activity Dock",
                IsActive = false,
                Lines = lines,
            },
        };
    }
}
