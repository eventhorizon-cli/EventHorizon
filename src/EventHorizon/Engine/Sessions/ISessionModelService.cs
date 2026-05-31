namespace EventHorizon.Engine.Sessions;

public interface ISessionModelService
{
    Task<SessionModelUpdateResult?> UpdateAsync(
        string sessionId,
        string? providerName,
        string? modelId,
        CancellationToken cancellationToken);
}
