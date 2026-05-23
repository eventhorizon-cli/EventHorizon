using EventHorizon.Configuration;
using EventHorizon.Pricing;

namespace EventHorizon.Terminal.Surfaces;

public sealed class CommandPaletteOverlayBuilder : ITerminalSurfaceBuilder
{
    public string SurfaceId => "overlay";

    public TerminalSurfaceViewModel Build(TerminalSurfaceBuildContext context)
    {
        IReadOnlyList<TerminalPaletteItem> filtered = TerminalCommandCatalog.FilterPaletteItems(context.PaletteItems, context.State.CommandPalette.Query);
        int selectedIndex = filtered.Count == 0 ? 0 : Math.Clamp(context.State.CommandPalette.SelectedIndex, 0, filtered.Count - 1);
        List<string> lines = [];

        if (filtered.Count == 0)
        {
            lines.Add("No command matches the current query.");
        }
        else
        {
            for (int index = 0; index < Math.Min(10, filtered.Count); index++)
            {
                TerminalPaletteItem item = filtered[index];
                var marker = index == selectedIndex ? "›" : " ";
                lines.Add($"{marker} {item.Title}");
                lines.Add($"  {item.Description}");
                if (!string.IsNullOrWhiteSpace(item.Footer))
                {
                    lines.Add($"  {item.Footer}");
                }
                lines.Add($"  {item.CommandText}");
                lines.Add(string.Empty);
            }
        }

        return new TerminalSurfaceViewModel
        {
            SurfaceId = SurfaceId,
            IsVisible = context.State.CommandPalette.IsOpen,
            Panel = new TerminalPanelViewModel
            {
                PanelId = "command-palette-overlay",
                Title = "Command Palette",
                IsActive = context.State.CommandPalette.IsOpen,
                Lines = lines,
            },
        };
    }
}