using EventHorizon.Protocols.Mcp;
using EventHorizon.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Providers;

public static class ProvidersServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonProviders(this IServiceCollection services)
    {
        services.AddSingleton<IToolCatalogFactory, ToolCatalog>();
        services.AddSingleton<IProviderChatClientFactory, ProviderChatClientFactory>();
        services.AddSingleton<IProviderAgentFactory, ProviderAgentFactory>();
        services.AddSingleton<McpToolConnector>();
        services.AddSingleton<IEventHorizonRuntimeFactory, EventHorizonRuntimeFactory>();
        services.AddHttpClient(nameof(EntryPoints.RemoteEventHorizonRuntimeFactory));
        return services;
    }
}

