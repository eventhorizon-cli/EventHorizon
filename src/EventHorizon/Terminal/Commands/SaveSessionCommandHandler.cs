namespace EventHorizon.Terminal.Commands;

public sealed class SaveSessionCommandHandler : ITerminalCommandHandler
{
    public bool CanHandle(TerminalCommand command) => command.Name == "/save";

    public async Task<TerminalCommandResult> ExecuteAsync(TerminalCommandContext context)
    {
        var sessionName = context.Command.Argument ?? context.Runtime.Options.Conversation.AutoSaveSessionName;
        await context.Runtime.SessionService.SaveAsync(sessionName, context.Runtime.State, context.CancellationToken).ConfigureAwait(false);
        await context.Runtime.RefreshSavedSessionsAsync(context.CancellationToken).ConfigureAwait(false);
        context.Runtime.State.SetActivePanel(TerminalPanelCatalog.Inspector);
        context.Runtime.State.AddActivity("save", "Session saved", sessionName);
        return TerminalCommandResult.Success($"Saved session '{sessionName}'.");
    }
}
