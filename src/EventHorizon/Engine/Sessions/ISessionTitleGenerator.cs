using EventHorizon.Engine.Sessions;

namespace EventHorizon.Engine.Sessions;

public interface ISessionTitleGenerator
{
    Task<string?> TryGenerateAsync(SessionDocument document, CancellationToken cancellationToken);
}

