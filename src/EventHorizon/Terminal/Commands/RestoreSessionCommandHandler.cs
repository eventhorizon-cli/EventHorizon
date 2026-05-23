namespace EventHorizon.Terminal.Commands;

public sealed class RestoreSessionCommandHandler : ITerminalCommandHandler
{
    public bool CanHandle(TerminalCommand command) => command.Name == "/restore";

    public async Task<TerminalCommandResult> ExecuteAsync(TerminalCommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Command.Argument))
        {
            return TerminalCommandResult.Success("Usage: /restore <session-id>");
        }

        TerminalSessionRestoreResult restored = await context.Runtime.SessionService.RestoreAsync(context.Command.Argument, context.CancellationToken).ConfigureAwait(false);
        context.Runtime.ReplaceState(restored.State);
        context.Runtime.NeedsTranscriptReplay = restored.RequiresTranscriptReplay;
        context.Runtime.UsageTracker.Restore(context.Runtime.State.TotalUsage, context.Runtime.State.TotalCost);
        await context.Runtime.RefreshSavedSessionsAsync(context.CancellationToken).ConfigureAwait(false);
        context.Runtime.State.SetActivePanel(TerminalPanelCatalog.Conversation);
        context.Runtime.State.AddActivity("restore", "Session restored", context.Command.Argument);
        return TerminalCommandResult.Success($"Restored session '{context.Command.Argument}'.");
    }
}
