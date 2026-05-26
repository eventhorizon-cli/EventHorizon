namespace EventHorizon.Terminal.Commands;

public sealed class DiffCommand : ITerminalCommand
{
    public string Name => "/diff";

    public string Description => "Show file changes";

    public Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
        => context.Dialogs.ShowDiffsAsync(context.State.Diffs);
}

