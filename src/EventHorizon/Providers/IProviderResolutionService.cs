using EventHorizon.Engine.Sessions;

namespace EventHorizon.Providers;

public interface IProviderResolutionService
{
    ResolvedProviderContext? TryResolveForSession(SessionDocument session);
}

