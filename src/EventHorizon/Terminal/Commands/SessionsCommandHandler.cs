namespace EventHorizon.Terminal.Commands;

public sealed class SessionsCommandHandler : ITerminalCommandHandler
{
    public bool CanHandle(TerminalCommand command) => command.Name == "/sessions";

    public async Task<TerminalCommandResult> ExecuteAsync(TerminalCommandContext context)
    {
        await context.Runtime.RefreshSavedSessionsAsync(context.CancellationToken).ConfigureAwait(false);
        context.Runtime.State.SetSidebarMode(TerminalSidebarModeCatalog.Sessions);
        context.Runtime.State.AddActivity("command", "Reloaded recent sessions", $"{context.Runtime.State.SavedSessions.Count} shown");
        return TerminalCommandResult.Success("Recent sessions refreshed in the sidebar.");
    }
}