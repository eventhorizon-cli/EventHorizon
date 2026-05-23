namespace EventHorizon.Terminal.Commands;

public sealed class StatsCommandHandler : ITerminalCommandHandler
{
    public bool CanHandle(TerminalCommand command) => command.Name == "/stats";

    public Task<TerminalCommandResult> ExecuteAsync(TerminalCommandContext context)
    {
        context.Runtime.State.SetActivePanel(TerminalPanelCatalog.Inspector);
        context.Runtime.State.AddActivity("command", "Usage stats refreshed");
        return Task.FromResult(TerminalCommandResult.Success("Inspector usage metrics refreshed."));
    }
}
