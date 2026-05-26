namespace EventHorizon.Terminal.Commands;

public sealed class FilesCommand : ITerminalCommand
{
    public string Name => "/files";

    public string Description => "Show context files";

    public Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
        => context.Dialogs.ShowContextFilesAsync(context.State.ContextFiles);
}

