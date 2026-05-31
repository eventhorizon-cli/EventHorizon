namespace EventHorizon.Engine.Events;

public sealed record ReasoningSummary(
    string Goal,
    IReadOnlyList<string> Plan,
    IReadOnlyList<string> Completed,
    string? Next,
    IReadOnlyList<string> Issues,
    IReadOnlyList<string> Decisions);

