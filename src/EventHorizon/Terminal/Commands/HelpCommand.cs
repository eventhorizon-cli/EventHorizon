namespace EventHorizon.Terminal.Commands;

public sealed class HelpCommand : ITerminalCommand
{
    public string Name => "/help";

    public string Description => "Show help";

    public Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
        => context.Dialogs.ShowHelpAsync();
}

