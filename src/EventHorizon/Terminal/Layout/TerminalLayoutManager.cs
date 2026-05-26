namespace EventHorizon.Terminal.Layout;

public sealed class TerminalLayoutManager
{
    public TerminalLayoutMode ResolveMode(TerminalSize size, TerminalLayoutMode? forcedMode = null)
    {
        if (forcedMode is { } explicitMode)
        {
            return explicitMode;
        }

        if (size.Width >= 150 && size.Height >= 40)
        {
            return TerminalLayoutMode.Expanded;
        }

        if (size.Width >= 100 && size.Height >= 30)
        {
            return TerminalLayoutMode.Standard;
        }

        return TerminalLayoutMode.Compact;
    }
}

