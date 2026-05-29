using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using EventHorizon.Diff;

namespace EventHorizon.AGUI;

internal sealed class RunStoreEntry
{
    private readonly object _gate = new();
    private readonly List<AGUIEventEnvelope> _history = [];
    private readonly ConcurrentDictionary<Guid, Channel<AGUIEventEnvelope>> _subscribers = new();

    public RunStoreEntry(AGUIRun run, FileStateTracker fileStateTracker, CancellationTokenSource cancellationTokenSource)
    {
        Run = run;
        FileStateTracker = fileStateTracker;
        CancellationTokenSource = cancellationTokenSource;
    }

    public AGUIRun Run { get; }

    public FileStateTracker FileStateTracker { get; }

    public CancellationTokenSource CancellationTokenSource { get; }

    public bool IsCompleted { get; private set; }

    public void Publish(AGUIEventEnvelope @event)
    {
        List<Channel<AGUIEventEnvelope>> subscribers;
        lock (_gate)
        {
            if (IsCompleted)
            {
                return;
            }

            @event.Sequence = _history.Count == 0 ? 1 : _history[^1].Sequence + 1;
            _history.Add(@event);
            subscribers = _subscribers.Values.ToList();
        }

        foreach (var subscriber in subscribers)
        {
            subscriber.Writer.TryWrite(@event);
        }
    }

    public void Complete()
    {
        List<Channel<AGUIEventEnvelope>> subscribers;
        lock (_gate)
        {
            if (IsCompleted)
            {
                return;
            }
            IsCompleted = true;
            subscribers = _subscribers.Values.ToList();
            _subscribers.Clear();
        }

        foreach (var subscriber in subscribers)
        {
            subscriber.Writer.TryComplete();
        }
    }


    public async IAsyncEnumerable<AGUIEventEnvelope> SubscribeAsync(long? afterSequence, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Channel<AGUIEventEnvelope>? channel = null;
        List<AGUIEventEnvelope> history;
        var subscriptionId = Guid.NewGuid();

        lock (_gate)
        {
            history = _history
                .Where(@event => !afterSequence.HasValue || @event.Sequence > afterSequence.Value)
                .ToList();
            if (!IsCompleted)
            {
                channel = Channel.CreateUnbounded<AGUIEventEnvelope>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                });
                _subscribers[subscriptionId] = channel;
            }
        }

        foreach (var @event in history)
        {
            yield return @event;
        }

        if (channel is null)
        {
            yield break;
        }

        try
        {
            await foreach (var @event in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                if (afterSequence.HasValue && @event.Sequence <= afterSequence.Value)
                {
                    continue;
                }

                yield return @event;
            }
        }
        finally
        {
            if (_subscribers.TryRemove(subscriptionId, out var subscriber))
            {
                subscriber.Writer.TryComplete();
            }
        }
    }
}

