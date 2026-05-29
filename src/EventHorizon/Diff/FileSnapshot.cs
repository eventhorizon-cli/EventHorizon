namespace EventHorizon.Diff;

public sealed record FileSnapshot(
    string Path,
    string? Content,
    byte[]? BinaryContent,
    string ContentHash,
    bool IsBinary,
    DateTimeOffset CapturedAt);

