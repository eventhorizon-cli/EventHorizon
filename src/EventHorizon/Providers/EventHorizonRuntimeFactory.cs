using EventHorizon.Context;
using EventHorizon.Prompting;
using EventHorizon.Protocols.Mcp;
using EventHorizon.Tools;
using EventHorizon.Workspace;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Providers;

public sealed class EventHorizonRuntimeFactory : IEventHorizonRuntimeFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISessionContextBuilder _sessionContextBuilder;
    private readonly IToolCatalogFactory _toolCatalogFactory;
    private readonly ISystemPromptFactory _systemPromptFactory;
    private readonly IProviderAgentFactory _providerAgentFactory;
    private readonly IProviderChatClientFactory _providerChatClientFactory;
    private readonly McpToolConnector _mcpToolConnector;

    public EventHorizonRuntimeFactory(
        IServiceScopeFactory scopeFactory,
        ISessionContextBuilder sessionContextBuilder,
        IToolCatalogFactory toolCatalogFactory,
        ISystemPromptFactory systemPromptFactory,
        IProviderAgentFactory providerAgentFactory,
        IProviderChatClientFactory providerChatClientFactory,
        McpToolConnector mcpToolConnector)
    {
        _scopeFactory = scopeFactory;
        _sessionContextBuilder = sessionContextBuilder;
        _toolCatalogFactory = toolCatalogFactory;
        _systemPromptFactory = systemPromptFactory;
        _providerAgentFactory = providerAgentFactory;
        _providerChatClientFactory = providerChatClientFactory;
        _mcpToolConnector = mcpToolConnector;
    }

    public async Task<IEventHorizonRuntime> CreateAsync(Configuration.AppOptions options, CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var workspaceService = services.GetRequiredService<WorkspaceService>();
        var toolCatalog = _toolCatalogFactory.Create(workspaceService, options);
        var allTools = new List<AITool>(toolCatalog.Select(static descriptor => descriptor.Tool));
        var contextSnapshot = await _sessionContextBuilder.BuildAsync(cancellationToken).ConfigureAwait(false);

        AgentSkillsProvider? skillsProvider = null;
        if (options.Agent.EnableSkills)
        {
            skillsProvider = new AgentSkillsProvider(services.GetRequiredService<WorkspaceSkill>());
        }

        IReadOnlyList<IAsyncDisposable> resources = [];
        if (options.Agent.EnableMcpTools)
        {
            var (mcpTools, mcpResources) = await _mcpToolConnector.ConnectAsync(options.McpServers, cancellationToken).ConfigureAwait(false);
            allTools.AddRange(mcpTools);
            resources = mcpResources;
        }

        var instructions = _systemPromptFactory.Build(options, contextSnapshot, toolCatalog);

        var modelName = options.Provider.Type.Equals("azure-openai", StringComparison.OrdinalIgnoreCase)
            ? (options.Provider.Deployment ?? options.Provider.Model ?? string.Empty)
            : (options.Provider.Model ?? string.Empty);

        var agent = _providerAgentFactory.CreateAgent(options, instructions, allTools, skillsProvider, services);
        return new EventHorizonRuntime(agent, services, modelName, contextSnapshot, toolCatalog, resources, scope);
    }
}

