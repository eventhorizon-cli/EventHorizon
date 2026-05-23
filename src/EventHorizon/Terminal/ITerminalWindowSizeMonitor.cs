namespace EventHorizon.Terminal;

public interface ITerminalWindowSizeMonitor : IDisposable
{
    event EventHandler? SizeChanged;

    void Start();
}

