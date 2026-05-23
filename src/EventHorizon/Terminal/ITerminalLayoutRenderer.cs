namespace EventHorizon.Terminal;

public interface ITerminalLayoutRenderer
{
    void Render(Terminal.TerminalViewModel viewModel);

    void Reset();
}

