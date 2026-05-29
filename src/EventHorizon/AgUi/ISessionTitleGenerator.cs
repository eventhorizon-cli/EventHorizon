using EventHorizon.Conversations;

namespace EventHorizon.AGUI;

public interface ISessionTitleGenerator
{
    Task<string?> TryGenerateAsync(ConversationSessionDocument document, CancellationToken cancellationToken);
}

