using EventHorizon.Context;
using EventHorizon.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Providers;

public sealed class EventHorizonRuntime : IEventHorizonRuntime
{
    private readonly IAsyncDisposable? _lifetime;

    public EventHorizonRuntime(
        AIAgent agent,
        IServiceProvider services,
        string modelName,
        string instructions,
        SessionContextSnapshot contextSnapshot,
        IReadOnlyList<ToolDescriptor> toolCatalog,
        IReadOnlyList<AITool> tools,
        AgentSkillsProvider? skillsProvider,
        IReadOnlyList<IAsyncDisposable> asyncResources,
        IAsyncDisposable? lifetime = null)
    {
        Agent = agent;
        Services = services;
        ModelName = modelName;
        Instructions = instructions;
        ContextSnapshot = contextSnapshot;
        ToolCatalog = toolCatalog;
        Tools = tools;
        SkillsProvider = skillsProvider;
        AsyncResources = asyncResources;
        _lifetime = lifetime;
    }

    public AIAgent Agent { get; }
    public IServiceProvider Services { get; }
    public string ModelName { get; }
    public string Instructions { get; }
    public SessionContextSnapshot ContextSnapshot { get; }
    public IReadOnlyList<ToolDescriptor> ToolCatalog { get; }
    public IReadOnlyList<AITool> Tools { get; }
    public AgentSkillsProvider? SkillsProvider { get; }
    public IReadOnlyList<IAsyncDisposable> AsyncResources { get; }

    public async ValueTask DisposeAsync()
    {
        foreach (var resource in AsyncResources.Reverse())
        {
            await resource.DisposeAsync().ConfigureAwait(false);
        }

        if (_lifetime is not null)
        {
            await _lifetime.DisposeAsync().ConfigureAwait(false);
        }
    }
}

