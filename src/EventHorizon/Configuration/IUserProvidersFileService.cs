namespace EventHorizon.Configuration;

public interface IUserProvidersFileService
{
    string FilePath { get; }

    void EnsureExists();

    void Save(AppOptions options);
}