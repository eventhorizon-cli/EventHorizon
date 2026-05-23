using EventHorizon.Configuration;
using EventHorizon.Conversations;
using EventHorizon.Terminal;

namespace EventHorizon.Conversations;

public interface IConversationSessionMapper
{
    ConversationSessionDocument MapToDocument(string name, AppOptions options, TerminalConversationState state, string? serializedSession);

    TerminalConversationState MapToState(ConversationSessionDocument document);
}

