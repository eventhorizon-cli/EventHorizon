using System.Collections.Concurrent;

namespace EventHorizon.Engine.Runs;

public sealed class RunStore
{
    private readonly ConcurrentDictionary<string, RunStoreEntry> _runs = new(StringComparer.Ordinal);

    internal RunStoreEntry Add(RunStoreEntry entry)
    {
        if (!_runs.TryAdd(entry.Run.Id, entry))
        {
            throw new InvalidOperationException($"A run with id '{entry.Run.Id}' already exists.");
        }

        return entry;
    }

    internal bool TryGet(string runId, out RunStoreEntry? entry)
        => _runs.TryGetValue(runId, out entry);
}

