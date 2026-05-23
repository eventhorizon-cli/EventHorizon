using System.Runtime.CompilerServices;
using EventHorizon.Pricing;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Execution;

public sealed class QueryLoop
{
    private readonly AIAgent _agent;
    private readonly SessionUsageTracker _usageTracker;

    public QueryLoop(AIAgent agent, SessionUsageTracker usageTracker)
    {
        _agent = agent;
        _usageTracker = usageTracker;
    }

    public async IAsyncEnumerable<QueryEvent> RunAsync(string prompt, AgentSession session, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _usageTracker.StartTurn();

        var assistantText = string.Empty;
        await foreach (Microsoft.Agents.AI.AgentResponseUpdate update in _agent.RunStreamingAsync([new ChatMessage(ChatRole.User, prompt)], session, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            _usageTracker.ObserveUpdate(update);

            foreach (QueryEvent activityEvent in StreamingActivityInspector.Inspect(update))
            {
                yield return activityEvent;
            }

            if (string.IsNullOrEmpty(update.Text))
            {
                continue;
            }

            assistantText += update.Text;
            yield return new QueryEvent(QueryEventKind.AssistantDelta, update.Text);
        }

        yield return new QueryEvent(
            QueryEventKind.Completed,
            assistantText,
            _usageTracker.LastTurnUsage,
            _usageTracker.LastTurnCost.HasPrice ? _usageTracker.LastTurnCost.TotalCost : null);
    }

    public void ResetUsage() => _usageTracker.Reset();
}

