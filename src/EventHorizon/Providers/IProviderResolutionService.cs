using EventHorizon.Conversations;

namespace EventHorizon.Providers;

public interface IProviderResolutionService
{
    IReadOnlyList<Configuration.ProviderOptions> GetProviderOptions();

    ResolvedProviderContext? TryResolveForSession(ConversationSessionDocument? session, ChatRequestOverrides? overrides = null);

    ResolvedProviderContext? TryResolveDefault();
}

