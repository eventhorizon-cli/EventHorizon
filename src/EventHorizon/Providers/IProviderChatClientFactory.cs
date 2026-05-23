using Microsoft.Extensions.AI;

namespace EventHorizon.Providers;

public interface IProviderChatClientFactory
{
    IChatClient CreateChatClient(Configuration.ProviderOptions options);
}

