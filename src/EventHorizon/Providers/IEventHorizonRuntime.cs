using EventHorizon.Context;
using EventHorizon.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Providers;

public interface IEventHorizonRuntime : IAsyncDisposable
{
    AIAgent Agent { get; }

    string ModelName { get; }

    string Instructions { get; }

    IServiceProvider Services { get; }

    SessionContextSnapshot ContextSnapshot { get; }

    IReadOnlyList<ToolDescriptor> ToolCatalog { get; }

    IReadOnlyList<AITool> Tools { get; }

    AgentSkillsProvider? SkillsProvider { get; }
}

