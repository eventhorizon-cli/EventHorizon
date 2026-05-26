using EventHorizon.Configuration;
using EventHorizon.Terminal.Dialogs;
using EventHorizon.Terminal.Views;

namespace EventHorizon.Terminal.Commands;

public sealed class TerminalCommandContext
{
    public required TerminalState State { get; init; }

    public required DialogService Dialogs { get; init; }

    public required TerminalEventDispatcher Dispatcher { get; init; }

    public required MainWindow MainWindow { get; init; }

    public required AppOptions Options { get; init; }
}

