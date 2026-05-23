using Microsoft.Agents.AI;

namespace EventHorizon.Conversations;

public sealed class ChatClientConversationSessionSerializer : IConversationSessionSerializer
{
    public string? Serialize(AgentSession session) => null;

    public AgentSession? Deserialize(string serializedSession) => null;
}

