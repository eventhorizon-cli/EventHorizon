namespace EventHorizon.AGUI;

public sealed record AGUIReasoningSummary(
    string Goal,
    IReadOnlyList<string> Plan,
    IReadOnlyList<string> Completed,
    string? Next,
    IReadOnlyList<string> Issues,
    IReadOnlyList<string> Decisions);

