using Microsoft.Agents.AI;

namespace EventHorizon.Conversations;

public interface IConversationSessionSerializer
{
    string? Serialize(AgentSession session);

    AgentSession? Deserialize(string serializedSession);
}

