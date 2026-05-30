using EventHorizon.Configuration;
using EventHorizon.Conversations;
using Microsoft.Agents.AI;

namespace EventHorizon.Providers;

public interface ISkillProviderFactory
{
    AgentSkillsProvider? Create(AppOptions options, IServiceProvider services, ConversationSessionDocument? sessionDocument = null);
}
