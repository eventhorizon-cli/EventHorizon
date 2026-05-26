using EventHorizon.Terminal.Models;

namespace EventHorizon.Terminal.Events;

public abstract record TerminalAgentEvent;

public sealed record AssistantDelta(string Text) : TerminalAgentEvent;

public sealed record AssistantMessageCompleted(string Message, int? TotalTokens = null, decimal? CostUsd = null) : TerminalAgentEvent;

public sealed record ToolCallStarted(
    string Id,
    string Name,
    string? Description,
    string? ArgumentsSummary) : TerminalAgentEvent;

public sealed record ToolCallOutput(string Id, string Output) : TerminalAgentEvent;

public sealed record ToolCallFinished(string Id, bool Success, string? Error) : TerminalAgentEvent;

public sealed record PlanUpdated(IReadOnlyList<TerminalPlanItem> Items) : TerminalAgentEvent;

public sealed record DiffUpdated(IReadOnlyList<TerminalDiffItem> Items) : TerminalAgentEvent;

public sealed record ContextFilesUpdated(IReadOnlyList<TerminalContextFile> Items) : TerminalAgentEvent;

public sealed record StatusChanged(TerminalRunStatus Status, string? Detail = null) : TerminalAgentEvent;

public sealed record AgentError(string Message, Exception? Exception) : TerminalAgentEvent;

