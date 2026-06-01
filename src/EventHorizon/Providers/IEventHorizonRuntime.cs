using Microsoft.Extensions.AI;

namespace EventHorizon.Providers;

public interface IEventHorizonRuntime : IAsyncDisposable
{
    ValueTask<string> GetInstructionsAsync(CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default);

    Task InvalidateAsync(CancellationToken cancellationToken);
}
