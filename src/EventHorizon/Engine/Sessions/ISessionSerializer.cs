using Microsoft.Agents.AI;

namespace EventHorizon.Engine.Sessions;

public interface ISessionSerializer
{
    string? Serialize(AgentSession session);

    AgentSession? Deserialize(string serializedSession);
}

