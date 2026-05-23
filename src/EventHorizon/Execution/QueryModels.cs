using Microsoft.Extensions.AI;

namespace EventHorizon.Execution;

public enum QueryEventKind
{
    UserMessage,
    ToolCall,
    ToolResult,
    AssistantDelta,
    Completed,
}

public sealed record QueryEvent(QueryEventKind Kind, string Text, UsageDetails? Usage = null, decimal? CostUsd = null);

public sealed record ConversationEntry(ChatRole Role, string Text);


