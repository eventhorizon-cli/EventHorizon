using EventHorizon.Context;
using EventHorizon.Providers;
using EventHorizon.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.EntryPoints;

internal sealed class EventHorizonRuntimeWrapper : IEventHorizonRuntime
{
    private readonly EventHorizonRuntimeHolder _holder;

    public EventHorizonRuntimeWrapper(EventHorizonRuntimeHolder holder)
    {
        _holder = holder;
    }

    private IEventHorizonRuntime Runtime => _holder.Runtime ?? throw new InvalidOperationException("Runtime not initialized");

    public AIAgent Agent => Runtime.Agent;
    public string ModelName => Runtime.ModelName;
    public IServiceProvider Services => Runtime.Services;
    public SessionContextSnapshot ContextSnapshot => Runtime.ContextSnapshot;
    public IReadOnlyList<ToolDescriptor> ToolCatalog => Runtime.ToolCatalog;

    public async ValueTask DisposeAsync()
    {
        await Runtime.DisposeAsync().ConfigureAwait(false);
    }
}
