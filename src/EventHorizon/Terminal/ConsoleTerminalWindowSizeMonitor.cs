namespace EventHorizon.Terminal;

public sealed class ConsoleTerminalWindowSizeMonitor : ITerminalWindowSizeMonitor
{
    private readonly object _syncLock = new();
    private Timer? _timer;
    private (int Width, int Height)? _lastSize;

    public event EventHandler? SizeChanged;

    public void Start()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return;
        }

        lock (_syncLock)
        {
            if (_timer is not null)
            {
                return;
            }

            _lastSize = TryReadSize();
            _timer = new Timer(CheckForChanges, null, dueTime: 100, period: 100);
        }
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    private void CheckForChanges(object? state)
    {
        (int Width, int Height)? nextSize = TryReadSize();
        if (nextSize is null)
        {
            return;
        }

        var changed = false;
        lock (_syncLock)
        {
            if (_lastSize is null || _lastSize.Value != nextSize.Value)
            {
                _lastSize = nextSize;
                changed = true;
            }
        }

        if (changed)
        {
            SizeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static (int Width, int Height)? TryReadSize()
    {
        try
        {
            return (Console.WindowWidth, Console.WindowHeight);
        }
        catch
        {
            return null;
        }
    }
}

