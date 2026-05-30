namespace EventHorizon.Configuration;

public interface IUserSkillsFileService
{
    string FilePath { get; }

    void EnsureExists();

    void Save(SkillsOptions options);
}
