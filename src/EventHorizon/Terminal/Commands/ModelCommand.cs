namespace EventHorizon.Terminal.Commands;

public sealed class ModelCommand : ITerminalCommand
{
    public string Name => "/model";

    public string Description => "Select the active model";

    public async Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
    {
        var model = await context.Dialogs.ShowModelSelectionAsync(context.State).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(model))
        {
            return;
        }

        context.State.CurrentModel = model;
        context.State.LastStatusMessage = $"Model switched to {model}.";
        context.MainWindow.RefreshFromState();
    }
}

