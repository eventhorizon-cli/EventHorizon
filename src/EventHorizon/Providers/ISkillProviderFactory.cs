using EventHorizon.Configuration;
using EventHorizon.Engine.Sessions;
using Microsoft.Agents.AI;

namespace EventHorizon.Providers;

public interface ISkillProviderFactory
{
    AgentSkillsProvider? Create(AgentOptions options, IServiceProvider services, SessionDocument? sessionDocument = null);
}
