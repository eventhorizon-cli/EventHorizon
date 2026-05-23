namespace EventHorizon.Terminal.Commands;

public sealed class FocusCommandHandler : ITerminalCommandHandler
{
    public bool CanHandle(TerminalCommand command) => command.Name == "/focus";

    public Task<TerminalCommandResult> ExecuteAsync(TerminalCommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Command.Argument))
        {
            context.Runtime.State.SetFocusedPath(null);
            context.Runtime.State.SetActivePanel(TerminalPanelCatalog.Explorer);
            context.Runtime.State.AddActivity("focus", "Explorer focus cleared");
            return Task.FromResult(TerminalCommandResult.Success("Explorer focus cleared."));
        }

        string focusedPath = TerminalWorkspacePathResolver.ResolveInsideWorkspace(context.Runtime.Options.WorkspaceRoot, context.Command.Argument);
        context.Runtime.State.SetFocusedPath(focusedPath);
        context.Runtime.State.SetActivePanel(TerminalPanelCatalog.Explorer);
        context.Runtime.State.SetSidebarMode(TerminalSidebarModeCatalog.Files);
        context.Runtime.State.AddActivity("focus", "Explorer focus updated", focusedPath);
        return Task.FromResult(TerminalCommandResult.Success($"Focused explorer on '{focusedPath}'."));
    }
}
