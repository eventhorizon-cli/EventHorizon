using EventHorizon.Context;
using EventHorizon.Tools;
using Microsoft.Agents.AI;

namespace EventHorizon.Providers;

public sealed class EventHorizonRuntime : IEventHorizonRuntime
{
    private readonly IAsyncDisposable? _lifetime;

    public EventHorizonRuntime(
        AIAgent agent,
        IServiceProvider services,
        string modelName,
        SessionContextSnapshot contextSnapshot,
        IReadOnlyList<ToolDescriptor> toolCatalog,
        IReadOnlyList<IAsyncDisposable> asyncResources,
        IAsyncDisposable? lifetime = null)
    {
        Agent = agent;
        Services = services;
        ModelName = modelName;
        ContextSnapshot = contextSnapshot;
        ToolCatalog = toolCatalog;
        AsyncResources = asyncResources;
        _lifetime = lifetime;
    }

    public AIAgent Agent { get; }
    public IServiceProvider Services { get; }
    public string ModelName { get; }
    public SessionContextSnapshot ContextSnapshot { get; }
    public IReadOnlyList<ToolDescriptor> ToolCatalog { get; }
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

