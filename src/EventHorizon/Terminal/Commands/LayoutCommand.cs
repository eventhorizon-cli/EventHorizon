using EventHorizon.Terminal.Layout;

namespace EventHorizon.Terminal.Commands;

public sealed class LayoutCommand : ITerminalCommand
{
    public string Name => "/layout";

    public string Description => "Change layout: auto / compact / standard / expanded";

    public async Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
    {
        var result = await context.Dialogs.ShowLayoutSelectionAsync(context.State.ActiveLayoutMode, cancellationToken).ConfigureAwait(false);
        context.Dispatcher.ApplyLayoutSelection(result);
    }
}

