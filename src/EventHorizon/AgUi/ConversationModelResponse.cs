namespace EventHorizon.AGUI;

public sealed class ConversationModelResponse
{
    public string ConversationId { get; set; } = string.Empty;

    public string? ProviderName { get; set; }

    public string ProviderType { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}

