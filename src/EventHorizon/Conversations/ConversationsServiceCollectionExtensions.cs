using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventHorizon.Conversations;

public static class ConversationsServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonConversations(this IServiceCollection services)
    {
        services.AddSingleton<IConversationSessionStore>(serviceProvider =>
            new FileConversationSessionStore(ResolveStoragePath(
                serviceProvider.GetRequiredService<IOptions<Configuration.AppOptions>>().Value,
                serviceProvider.GetRequiredService<Workspace.WorkspaceContext>())));
        return services;
    }

    private static string ResolveStoragePath(Configuration.AppOptions options, Workspace.WorkspaceContext workspaceContext)
    {
        if (!string.IsNullOrWhiteSpace(options.Conversation.StoragePath))
        {
            return Path.GetFullPath(options.Conversation.StoragePath);
        }

        return Path.Combine(workspaceContext.WorkspaceRoot, ".eventhorizon", "sessions");
    }
}

