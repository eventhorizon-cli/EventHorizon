using EventHorizon.DTOs;
using EventHorizon.Engine.Events;
using EventHorizon.Workspace.Diff;

namespace EventHorizon.Engine.Runs;

public interface IRunService
{
    Task<RunDTO> CreateAsync(CreateRunRequestDTO request, CancellationToken cancellationToken);

    RunDTO? Get(string sessionId, string runId);

    bool Cancel(string sessionId, string runId);

    IAsyncEnumerable<EventEnvelope>? StreamEventsAsync(string sessionId, string runId, long? afterSequence, CancellationToken cancellationToken);

    IReadOnlyList<FileChange>? GetChanges(string sessionId, string runId, CancellationToken cancellationToken);

    FileDiff? GetDiff(string sessionId, string runId, string path, CancellationToken cancellationToken);
}
