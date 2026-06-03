using EventHorizon.Configuration;
using EventHorizon.Engine.Sessions;

namespace EventHorizon.Prompting;

public interface ISystemPromptFactory
{
    string Build(AgentOptions options, SessionContextSnapshot snapshot);
}
