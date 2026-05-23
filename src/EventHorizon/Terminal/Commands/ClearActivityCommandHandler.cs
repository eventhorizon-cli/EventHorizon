namespace EventHorizon.Terminal.Commands;

public sealed class ClearActivityCommandHandler : ITerminalCommandHandler
{
    public bool CanHandle(TerminalCommand command) => command.Name == "/clear";

    public Task<TerminalCommandResult> ExecuteAsync(TerminalCommandContext context)
    {
        context.Runtime.State.SetActivePanel(TerminalPanelCatalog.Activity);
        context.Runtime.State.ClearActivity();
        context.Runtime.State.AddActivity("command", "Activity feed cleared");
        return Task.FromResult(TerminalCommandResult.Success("Activity feed cleared."));
    }
}