namespace EventHorizon.Terminal;

public sealed record TerminalSessionRestoreResult(TerminalConversationState State, bool RequiresTranscriptReplay);

