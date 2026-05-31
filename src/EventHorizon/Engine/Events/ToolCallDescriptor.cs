namespace EventHorizon.Engine.Events;

public sealed record ToolCallDescriptor(
    string Id,
    string Name,
    string? Arguments,
    string Status,
    string? Result = null);

