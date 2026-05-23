using EventHorizon.Configuration;
using EventHorizon.Context;
using EventHorizon.Providers;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.EntryPoints;

internal sealed class RemoteEventHorizonRuntimeFactory : IRemoteEventHorizonRuntimeFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;

    public RemoteEventHorizonRuntimeFactory(IHttpClientFactory httpClientFactory, IServiceScopeFactory scopeFactory)
    {
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
    }

    public IEventHorizonRuntime Create(AppOptions options)
    {
        var httpClient = _httpClientFactory.CreateClient(nameof(RemoteEventHorizonRuntimeFactory));
        httpClient.Timeout = TimeSpan.FromMinutes(5);
        var chatClient = new AGUIChatClient(httpClient, options.Protocol.ClientUrl);
        var remoteAgent = chatClient.AsAIAgent(name: "eventhorizon-remote", description: "Remote AGUI coding agent");
        var snapshot = new Context.SessionContextSnapshot(
            CurrentDate: $"Today's date is {DateTimeOffset.Now:yyyy-MM-dd}.",
            WorkspaceRoot: options.WorkspaceRoot,
            WorkspaceSummary: "Remote AGUI client mode does not inspect the local workspace.",
            GitStatus: "Git status unavailable in remote client mode.",
            ProjectInstructions: "The remote agent controls its own instructions and tools.");
        var scope = _scopeFactory.CreateAsyncScope();
        return new EventHorizonRuntime(remoteAgent, scope.ServiceProvider, options.Provider.Model ?? "remote-agui", snapshot, [], [], scope);
    }
}
