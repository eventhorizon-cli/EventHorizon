using Microsoft.Agents.AI;

namespace EventHorizon.Engine.Sessions;

public sealed class ChatClientSessionSerializer : ISessionSerializer
{
    public string? Serialize(AgentSession session) => null;

    public AgentSession? Deserialize(string serializedSession) => null;
}

