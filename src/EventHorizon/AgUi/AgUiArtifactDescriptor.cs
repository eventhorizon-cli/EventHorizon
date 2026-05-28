namespace EventHorizon.AGUI;

public sealed record AGUIArtifactDescriptor(
    string Id,
    string Kind,
    string Label,
    string? Path,
    object? Metadata);

