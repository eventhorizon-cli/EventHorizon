namespace EventHorizon.AGUI;

public sealed class ConversationModelUpdateResult
{
    public required string ConversationId { get; init; }

    public string? ProviderName { get; init; }

    public required string ProviderType { get; init; }

    public required string ModelId { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
