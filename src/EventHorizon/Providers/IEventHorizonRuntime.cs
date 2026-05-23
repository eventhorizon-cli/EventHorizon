using EventHorizon.Context;
using EventHorizon.Tools;
using Microsoft.Agents.AI;

namespace EventHorizon.Providers;

public interface IEventHorizonRuntime : IAsyncDisposable
{
    AIAgent Agent { get; }

    string ModelName { get; }

    IServiceProvider Services { get; }

    SessionContextSnapshot ContextSnapshot { get; }

    IReadOnlyList<ToolDescriptor> ToolCatalog { get; }
}

