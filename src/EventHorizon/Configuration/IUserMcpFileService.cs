namespace EventHorizon.Configuration;

public interface IUserMcpFileService
{
    string FilePath { get; }

    void EnsureExists();

    void Save(McpOptions options);
}
