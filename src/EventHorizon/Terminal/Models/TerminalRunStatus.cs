namespace EventHorizon.Terminal.Models;

public enum TerminalRunStatus
{
    Idle,
    Thinking,
    Streaming,
    ToolRunning,
    WaitingForInput,
    Cancelled,
    Error,
}

