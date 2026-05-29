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
        services.AddSingleton<IProviderResolutionService, ProviderResolutionService>();
        services.AddSingleton<IConversationAgentManager, ConversationAgentManager>();
        services.AddSingleton<McpToolConnector>();
        return services;
    }
}

