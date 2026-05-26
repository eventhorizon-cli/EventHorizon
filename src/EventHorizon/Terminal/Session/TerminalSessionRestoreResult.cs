namespace EventHorizon.Terminal.Session;

public sealed record TerminalSessionRestoreResult(TerminalState State, bool RequiresTranscriptReplay);

