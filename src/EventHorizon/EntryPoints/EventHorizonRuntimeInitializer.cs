using EventHorizon.Configuration;
using EventHorizon.Context;
using EventHorizon.Prompting;
using EventHorizon.Protocols.Mcp;
using EventHorizon.Providers;
using EventHorizon.Tools;
using EventHorizon.Workspace;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventHorizon.EntryPoints;

internal sealed class EventHorizonRuntimeInitializer : IEventHorizonRuntimeInitializer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISessionContextBuilder _sessionContextBuilder;
    private readonly IToolCatalogFactory _toolCatalogFactory;
    private readonly ISystemPromptFactory _systemPromptFactory;
    private readonly IProviderAgentFactory _providerAgentFactory;
    private readonly ISkillProviderFactory _skillProviderFactory;
    private readonly McpToolConnector _mcpToolConnector;
    private readonly IOptions<AppOptions> _options;
    private readonly EventHorizonRuntimeHolder _runtimeHolder;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public EventHorizonRuntimeInitializer(
        IServiceScopeFactory scopeFactory,
        ISessionContextBuilder sessionContextBuilder,
        IToolCatalogFactory toolCatalogFactory,
        ISystemPromptFactory systemPromptFactory,
        IProviderAgentFactory providerAgentFactory,
        ISkillProviderFactory skillProviderFactory,
        McpToolConnector mcpToolConnector,
        IOptions<AppOptions> options,
        EventHorizonRuntimeHolder runtimeHolder)
    {
        _scopeFactory = scopeFactory;
        _sessionContextBuilder = sessionContextBuilder;
        _toolCatalogFactory = toolCatalogFactory;
        _systemPromptFactory = systemPromptFactory;
        _providerAgentFactory = providerAgentFactory;
        _skillProviderFactory = skillProviderFactory;
        _mcpToolConnector = mcpToolConnector;
        _options = options;
        _runtimeHolder = runtimeHolder;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_runtimeHolder.Runtime is not null)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_runtimeHolder.Runtime is not null)
            {
                return;
            }

            _runtimeHolder.Runtime = await CreateLocalRuntimeAsync(_options.Value, cancellationToken).ConfigureAwait(false);
            _runtimeHolder.InitializationError = null;
        }
        catch (Exception ex)
        {
            _runtimeHolder.InitializationError = ex;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IEventHorizonRuntime> CreateLocalRuntimeAsync(AppOptions options, CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var workspaceService = services.GetRequiredService<WorkspaceService>();
        var toolCatalog = _toolCatalogFactory.Create(workspaceService, options);
        var allTools = new List<AITool>(toolCatalog.Select(static descriptor => descriptor.Tool));
        var contextSnapshot = await _sessionContextBuilder.BuildAsync(cancellationToken).ConfigureAwait(false);

        var skillsProvider = _skillProviderFactory.Create(options, services);

        IReadOnlyList<IAsyncDisposable> resources = [];
        if (options.Agent.EnableMcpTools)
        {
            var (mcpTools, mcpResources) = await _mcpToolConnector.ConnectAsync(options.McpServers, cancellationToken).ConfigureAwait(false);
            allTools.AddRange(mcpTools);
            resources = mcpResources;
        }

        var instructions = _systemPromptFactory.Build(options, contextSnapshot, toolCatalog);
        var modelName = string.Equals(options.Provider.Type, "azure-openai", StringComparison.OrdinalIgnoreCase)
            ? (options.Provider.Deployment ?? options.Provider.Model ?? string.Empty)
            : (options.Provider.Model ?? string.Empty);

        var agent = _providerAgentFactory.CreateAgent(options, instructions, allTools, skillsProvider, services);
        return new EventHorizonRuntime(agent, services, modelName, instructions, contextSnapshot, toolCatalog, allTools, skillsProvider, resources, scope);
    }
}
