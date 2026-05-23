namespace EventHorizon.Terminal.Panels;

public sealed class ExplorerPanelBuilder : ITerminalPanelBuilder
{
    public string PanelId => TerminalPanelCatalog.Explorer;

    public TerminalPanelViewModel Build(TerminalPanelBuildContext context) => new()
    {
        PanelId = PanelId,
        Title = "Explorer",
        IsActive = string.Equals(context.State.ActivePanelId, PanelId, StringComparison.OrdinalIgnoreCase),
        Lines = WorkspaceExplorerSnapshotBuilder.Build(context.Options.WorkspaceRoot, context.State.FocusedPath)
    };
}
