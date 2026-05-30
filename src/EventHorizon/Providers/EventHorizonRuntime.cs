using EventHorizon.Configuration;
using EventHorizon.Context;
using EventHorizon.Prompting;
using EventHorizon.Protocols.Mcp;
using EventHorizon.Tools;
using EventHorizon.Workspace;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace EventHorizon.Providers;

public sealed class EventHorizonRuntime : IEventHorizonRuntime
{
    private readonly WorkspaceService _workspaceService;
    private readonly ISessionContextBuilder _sessionContextBuilder;
    private readonly IToolCatalogFactory _toolCatalogFactory;
    private readonly ISystemPromptFactory _systemPromptFactory;
    private readonly McpToolConnector _mcpToolConnector;
    private readonly IOptionsMonitor<AppOptions> _appOptionsMonitor;
    private readonly IOptionsMonitor<McpOptions> _mcpOptionsMonitor;
    private readonly SemaphoreSlim _mcpLock = new(1, 1);
    private IReadOnlyList<AITool>? _mcpTools;
    private IReadOnlyList<IAsyncDisposable> _mcpResources = [];

    public EventHorizonRuntime(
        WorkspaceService workspaceService,
        ISessionContextBuilder sessionContextBuilder,
        IToolCatalogFactory toolCatalogFactory,
        ISystemPromptFactory systemPromptFactory,
        McpToolConnector mcpToolConnector,
        IOptionsMonitor<AppOptions> appOptionsMonitor,
        IOptionsMonitor<McpOptions> mcpOptionsMonitor)
    {
        _workspaceService = workspaceService;
        _sessionContextBuilder = sessionContextBuilder;
        _toolCatalogFactory = toolCatalogFactory;
        _systemPromptFactory = systemPromptFactory;
        _mcpToolConnector = mcpToolConnector;
        _appOptionsMonitor = appOptionsMonitor;
        _mcpOptionsMonitor = mcpOptionsMonitor;
    }

    public string ModelName
    {
        get
        {
            var provider = _appOptionsMonitor.CurrentValue.Provider;
            return string.Equals(provider.Type, "azure-openai", StringComparison.OrdinalIgnoreCase)
                ? provider.Deployment ?? provider.Model ?? string.Empty
                : provider.Model ?? string.Empty;
        }
    }

    public async ValueTask<SessionContextSnapshot> GetContextSnapshotAsync(CancellationToken cancellationToken = default)
        => await _sessionContextBuilder.BuildAsync(cancellationToken).ConfigureAwait(false);

    public async ValueTask<string> GetInstructionsAsync(CancellationToken cancellationToken = default)
    {
        var appOptions = _appOptionsMonitor.CurrentValue;
        var context = await _sessionContextBuilder.BuildAsync(cancellationToken).ConfigureAwait(false);
        var toolCatalog = _toolCatalogFactory.Create(_workspaceService, appOptions);
        return _systemPromptFactory.Build(appOptions, context, toolCatalog);
    }

    public ValueTask<IReadOnlyList<ToolDescriptor>> GetToolCatalogAsync(CancellationToken cancellationToken = default)
    {
        var appOptions = _appOptionsMonitor.CurrentValue;
        var catalog = _toolCatalogFactory.Create(_workspaceService, appOptions);
        return ValueTask.FromResult(catalog);
    }

    public async ValueTask<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        var appOptions = _appOptionsMonitor.CurrentValue;
        var toolCatalog = _toolCatalogFactory.Create(_workspaceService, appOptions);
        var tools = new List<AITool>(toolCatalog.Select(static d => d.Tool));

        if (appOptions.Agent.EnableMcpTools)
        {
            var mcpTools = await GetOrConnectMcpToolsAsync(cancellationToken).ConfigureAwait(false);
            tools.AddRange(mcpTools);
        }

        return tools;
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken)
    {
        await _mcpLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await DisposeMcpResourcesAsync().ConfigureAwait(false);
            _mcpTools = null;
        }
        finally
        {
            _mcpLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _mcpLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await DisposeMcpResourcesAsync().ConfigureAwait(false);
        }
        finally
        {
            _mcpLock.Release();
        }
    }

    private async Task<IReadOnlyList<AITool>> GetOrConnectMcpToolsAsync(CancellationToken cancellationToken)
    {
        await _mcpLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_mcpTools is not null)
            {
                return _mcpTools;
            }

            var (tools, resources) = await _mcpToolConnector
                .ConnectAsync(_mcpOptionsMonitor.CurrentValue.Servers, cancellationToken)
                .ConfigureAwait(false);

            _mcpTools = tools;
            _mcpResources = resources;
            return _mcpTools;
        }
        finally
        {
            _mcpLock.Release();
        }
    }

    private async Task DisposeMcpResourcesAsync()
    {
        foreach (var resource in _mcpResources.Reverse())
        {
            await resource.DisposeAsync().ConfigureAwait(false);
        }

        _mcpResources = [];
    }
}
