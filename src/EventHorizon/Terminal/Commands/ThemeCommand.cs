namespace EventHorizon.Terminal.Commands;

public sealed class ThemeCommand : ITerminalCommand
{
    public string Name => "/theme";

    public string Description => "Switch color theme";

    public async Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
    {
        var theme = await context.Dialogs.ShowThemeSelectionAsync(context.State.CurrentThemeName, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(theme))
        {
            context.State.CurrentThemeName = theme;
            context.State.LastStatusMessage = $"Theme switched to {theme}.";
            context.MainWindow.RefreshFromState();
        }
    }
}

