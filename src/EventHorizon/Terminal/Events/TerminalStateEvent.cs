using EventHorizon.Terminal.Models;

namespace EventHorizon.Terminal.Events;

public abstract record TerminalStateEvent;

public sealed record TerminalStatusChanged(TerminalRunStatus Status) : TerminalStateEvent;

public sealed record TerminalErrorRaised(string Message) : TerminalStateEvent;

