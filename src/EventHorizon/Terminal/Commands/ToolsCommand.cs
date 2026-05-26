namespace EventHorizon.Terminal.Commands;

public sealed class ToolsCommand : ITerminalCommand
{
    public string Name => "/tools";

    public string Description => "Show tool calls";

    public Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
        => context.Dialogs.ShowToolCallsAsync(context.State.ToolCalls);
}

