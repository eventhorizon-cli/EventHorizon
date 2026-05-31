using EventHorizon.Configuration;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Providers;

public interface IProviderAgentFactory
{
    AIAgent CreateAgent(
        AgentOptions agentOptions,
        ProviderOptions providerOptions,
        string instructions,
        IReadOnlyList<AITool> tools,
        AgentSkillsProvider? skillsProvider,
        IServiceProvider services);
}
