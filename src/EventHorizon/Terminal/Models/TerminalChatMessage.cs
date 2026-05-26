namespace EventHorizon.Terminal.Models;

public enum TerminalMessageRole
{
    System,
    User,
    Assistant,
    Tool,
    Error,
}

public sealed record TerminalChatMessage(
    TerminalMessageRole Role,
    string Content,
    DateTimeOffset CreatedAt);

