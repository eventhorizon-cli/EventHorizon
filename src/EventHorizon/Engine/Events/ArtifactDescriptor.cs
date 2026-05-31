namespace EventHorizon.Engine.Events;

public sealed record ArtifactDescriptor(
    string Id,
    string Kind,
    string Label,
    string? Path,
    object? Metadata);

