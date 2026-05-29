namespace EventHorizon.Configuration;

public interface IAppConfigurationService
{
    AppOptions Get();

    Task<AppOptions> SaveAsync(AppOptions options, CancellationToken cancellationToken);

    Task<AppOptions> SetDefaultProviderAsync(string? providerName, CancellationToken cancellationToken);
}

