namespace EventHorizon.Engine.Events;

public sealed record MessageDescriptor(
    string Id,
    string Role,
    string Content);

