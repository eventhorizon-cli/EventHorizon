using System.Runtime.CompilerServices;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Execution;

public sealed class QueryEngine
{
    private readonly IEventHorizonRuntime _runtime;
    private readonly ISessionUsageTracker _usageTracker;
    private readonly List<ConversationEntry> _history = [];
    private AgentSession? _session;

    public QueryEngine(IEventHorizonRuntime runtime, ISessionUsageTracker usageTracker)
    {
        _runtime = runtime;
        _usageTracker = usageTracker;
    }

    public IReadOnlyList<ConversationEntry> History => _history;

    public void LoadConversationState(AgentSession? session, IEnumerable<ConversationEntry> history)
    {
        _session = session;
        _history.Clear();
        _history.AddRange(history);
    }

    public async IAsyncEnumerable<QueryEvent> SubmitAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _session ??= await _runtime.Agent.CreateSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        _history.Add(new ConversationEntry(ChatRole.User, prompt));
        yield return new QueryEvent(QueryEventKind.UserMessage, prompt);

        var completedAssistantText = string.Empty;
        QueryLoop turnLoop = new(_runtime.Agent, _usageTracker);
        await foreach (var queryEvent in turnLoop.RunAsync(prompt, _session, cancellationToken).ConfigureAwait(false))
        {
            if (queryEvent.Kind == QueryEventKind.Completed)
            {
                completedAssistantText = queryEvent.Text;
            }

            yield return queryEvent;
        }

        _history.Add(new ConversationEntry(ChatRole.Assistant, completedAssistantText));
    }

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        _history.Clear();
        _usageTracker.Reset();
        _session = await _runtime.Agent.CreateSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}


