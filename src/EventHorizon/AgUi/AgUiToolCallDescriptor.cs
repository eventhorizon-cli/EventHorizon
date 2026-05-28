namespace EventHorizon.AGUI;

public sealed record AGUIToolCallDescriptor(
    string Id,
    string Name,
    string? Arguments,
    string Status,
    string? Result = null);

