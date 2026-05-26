namespace EventHorizon.Terminal.Events;

public abstract record TerminalInputEvent;

public sealed record SubmitInputRequested(string Text) : TerminalInputEvent;

public sealed record CommandPaletteRequested() : TerminalInputEvent;

public sealed record CancelRequested() : TerminalInputEvent;

