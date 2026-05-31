using System.Text.Json;
using System.Text.Json.Nodes;

namespace EventHorizon.Configuration;

public sealed class UserMcpFileService : IUserMcpFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public UserMcpFileService(IPathEnvironment pathEnvironment)
    {
        FilePath = GetDefaultFilePath(pathEnvironment);
    }

    public string FilePath { get; }

    public void EnsureExists()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        if (!File.Exists(FilePath))
        {
            SafeWrite("{}" + Environment.NewLine);
        }
    }

    public void Save(McpOptions options)
    {
        EnsureExists();

        var persisted = new JsonObject
        {
            ["McpServers"] = new JsonObject
            {
                [nameof(McpOptions.Servers)] = JsonSerializer.SerializeToNode(
                    options.Servers.Select(CloneMcpServer).ToList(),
                    EventHorizonJsonContext.Default.ListMcpServerOptions),
            },
        };

        SafeWrite(persisted.ToJsonString(JsonOptions) + Environment.NewLine);
    }

    public static string GetDefaultFilePath(IPathEnvironment pathEnvironment)
        => Path.Combine(pathEnvironment.HomeDirectory, ".eventhorizon", "mcp.json");

    private void SafeWrite(string content)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        var tempFile = Path.Combine(directory, $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tempFile, content);

        try
        {
            if (File.Exists(FilePath))
            {
                File.Move(tempFile, FilePath, overwrite: true);
            }
            else
            {
                File.Move(tempFile, FilePath);
            }
        }
        catch
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            throw;
        }
    }

    private static McpServerOptions CloneMcpServer(McpServerOptions server)
        => new()
        {
            Name = server.Name,
            Command = server.Command,
            Arguments = [.. server.Arguments],
            Url = server.Url,
            EnvironmentVariables = new Dictionary<string, string>(server.EnvironmentVariables, StringComparer.OrdinalIgnoreCase),
            Enabled = server.Enabled,
        };
}
