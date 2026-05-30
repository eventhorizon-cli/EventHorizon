namespace EventHorizon.AGUI;

public interface IConversationModelService
{
    Task<ConversationModelUpdateResult?> UpdateAsync(
        string conversationId,
        string? providerName,
        string? modelId,
        CancellationToken cancellationToken);
}
