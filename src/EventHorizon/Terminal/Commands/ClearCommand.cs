namespace EventHorizon.Terminal.Commands;

public sealed class ClearCommand : ITerminalCommand
{
    public string Name => "/clear";

    public string Description => "Clear chat";

    public Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
    {
        context.State.ClearChat();
        context.State.LastStatusMessage = "Chat cleared.";
        context.MainWindow.RefreshFromState();
        return Task.CompletedTask;
    }
}

