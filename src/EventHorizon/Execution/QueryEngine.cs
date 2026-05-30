using System.Runtime.CompilerServices;
using EventHorizon.Configuration;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace EventHorizon.Execution;

public sealed class QueryEngine
{
    private readonly IEventHorizonRuntime _runtime;
    private readonly IProviderAgentFactory _providerAgentFactory;
    private readonly ISkillProviderFactory _skillProviderFactory;
    private readonly IOptionsMonitor<AppOptions> _appOptionsMonitor;
    private readonly IServiceProvider _services;
    private readonly ISessionUsageTracker _usageTracker;
    private readonly List<ConversationEntry> _history = [];
    private AgentSession? _session;

    public QueryEngine(
        IEventHorizonRuntime runtime,
        IProviderAgentFactory providerAgentFactory,
        ISkillProviderFactory skillProviderFactory,
        IOptionsMonitor<AppOptions> appOptionsMonitor,
        IServiceProvider services,
        ISessionUsageTracker usageTracker)
    {
        _runtime = runtime;
        _providerAgentFactory = providerAgentFactory;
        _skillProviderFactory = skillProviderFactory;
        _appOptionsMonitor = appOptionsMonitor;
        _services = services;
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
        var agent = await BuildAgentAsync(cancellationToken).ConfigureAwait(false);
        _session ??= await agent.CreateSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        _history.Add(new ConversationEntry(ChatRole.User, prompt));
        yield return new QueryEvent(QueryEventKind.UserMessage, prompt);

        var completedAssistantText = string.Empty;
        QueryLoop turnLoop = new(agent, _usageTracker);
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
        var agent = await BuildAgentAsync(cancellationToken).ConfigureAwait(false);
        _session = await agent.CreateSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<AIAgent> BuildAgentAsync(CancellationToken cancellationToken)
    {
        var appOptions = _appOptionsMonitor.CurrentValue;
        var instructions = await _runtime.GetInstructionsAsync(cancellationToken).ConfigureAwait(false);
        var tools = await _runtime.GetToolsAsync(cancellationToken).ConfigureAwait(false);
        var skillsProvider = _skillProviderFactory.Create(appOptions, _services);
        return _providerAgentFactory.CreateAgent(appOptions, instructions, tools, skillsProvider, _services);
    }
}
