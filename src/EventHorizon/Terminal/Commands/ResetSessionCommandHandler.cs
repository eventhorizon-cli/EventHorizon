namespace EventHorizon.Terminal.Commands;

public sealed class ResetSessionCommandHandler : ITerminalCommandHandler
{
    public bool CanHandle(TerminalCommand command) => command.Name == "/reset";

    public async Task<TerminalCommandResult> ExecuteAsync(TerminalCommandContext context)
    {
        await context.Runtime.SessionService.ResetAsync(context.CancellationToken).ConfigureAwait(false);
        context.Runtime.NeedsTranscriptReplay = false;
        context.Runtime.UsageTracker.Reset();
        context.Runtime.ReplaceState(new TerminalConversationState());
        await context.Runtime.RefreshSavedSessionsAsync(context.CancellationToken).ConfigureAwait(false);
        context.Runtime.State.SetActivePanel(TerminalPanelCatalog.Conversation);
        context.Runtime.State.AddActivity("reset", "Started a new session");
        return TerminalCommandResult.Success("Started a fresh session.");
    }
}