using EventHorizon.Engine.Sessions;
using EventHorizon.Tools;
using Microsoft.Extensions.AI;

namespace EventHorizon.Providers;

public interface IEventHorizonRuntime : IAsyncDisposable
{
    string ModelName { get; }

    ValueTask<SessionContextSnapshot> GetContextSnapshotAsync(CancellationToken cancellationToken = default);

    ValueTask<string> GetInstructionsAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<ToolDescriptor> GetToolCatalog(CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default);

    Task InvalidateAsync(CancellationToken cancellationToken);
}
