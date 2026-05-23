using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventHorizon.Conversations;

public static class ConversationsServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonConversations(this IServiceCollection services)
    {
        services.AddSingleton<IConversationSessionMapper, ConversationSessionMapper>();
        services.AddSingleton<IConversationSessionSerializer, ChatClientConversationSessionSerializer>();
        services.AddSingleton<IConversationSessionStore>(serviceProvider =>
            new FileConversationSessionStore(ResolveStoragePath(serviceProvider.GetRequiredService<IOptions<Configuration.AppOptions>>().Value)));
        return services;
    }

    private static string ResolveStoragePath(Configuration.AppOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Conversation.StoragePath))
        {
            return Path.GetFullPath(options.Conversation.StoragePath);
        }

        return Path.Combine(Path.GetFullPath(options.WorkspaceRoot), ".eventhorizon", "sessions");
    }
}

