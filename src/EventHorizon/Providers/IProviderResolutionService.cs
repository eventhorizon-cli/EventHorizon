using EventHorizon.Engine.Sessions;

namespace EventHorizon.Providers;

public interface IProviderResolutionService
{
    IReadOnlyList<Configuration.ProviderOptions> GetProviderOptions();

    ResolvedProviderContext? TryResolveForSession(SessionDocument? session, ChatRequestOverrides? overrides = null);

    ResolvedProviderContext? TryResolveDefault();
}

