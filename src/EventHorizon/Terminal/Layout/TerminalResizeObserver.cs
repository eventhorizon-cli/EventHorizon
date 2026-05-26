using EventHorizon.Terminal.Views;

namespace EventHorizon.Terminal.Layout;

public sealed class TerminalResizeObserver
{
    public event EventHandler<TerminalSize>? Resized;

    public void Attach(MainWindow window)
        => window.FrameChanged += (_, _) => Resized?.Invoke(this, new TerminalSize(window.Frame.Width, window.Frame.Height));
}

