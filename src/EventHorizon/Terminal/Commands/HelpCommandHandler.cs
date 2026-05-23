namespace EventHorizon.Terminal.Commands;

public sealed class HelpCommandHandler : ITerminalCommandHandler
{
    public bool CanHandle(TerminalCommand command) => command.Name == "/help";

    public Task<TerminalCommandResult> ExecuteAsync(TerminalCommandContext context)
    {
        context.Runtime.State.OpenCommandPalette();
        context.Runtime.State.SetSidebarMode(TerminalSidebarModeCatalog.Commands);
        context.Runtime.State.AddActivity("command", "Opened command palette overlay");
        return Task.FromResult(TerminalCommandResult.Success("Command palette opened."));
    }
}