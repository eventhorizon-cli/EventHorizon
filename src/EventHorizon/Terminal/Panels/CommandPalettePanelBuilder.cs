namespace EventHorizon.Terminal.Panels;

public sealed class CommandPalettePanelBuilder : ITerminalPanelBuilder
{
    public string PanelId => TerminalPanelCatalog.Commands;

    public TerminalPanelViewModel Build(TerminalPanelBuildContext context) => new()
    {
        PanelId = PanelId,
        Title = "Command Palette",
        IsActive = string.Equals(context.State.ActivePanelId, PanelId, StringComparison.OrdinalIgnoreCase),
        Lines = TerminalCommandCatalog.BuildPaletteLines(context.State.CommandHistory, context.State.FocusedPath)
    };
}
