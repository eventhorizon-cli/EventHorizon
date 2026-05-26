namespace EventHorizon.Configuration;

internal interface IAppOptionsInitializer
{
    void Initialize(AppOptions options);

    void RefreshActiveProvider(AppOptions options);
}

