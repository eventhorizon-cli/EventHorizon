namespace EventHorizon.Engine.Sessions;

public sealed class SessionModelUpdateResult
{
    public required string SessionId { get; init; }

    public string? ProviderName { get; init; }

    public required string ProviderType { get; init; }

    public required string ModelId { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
