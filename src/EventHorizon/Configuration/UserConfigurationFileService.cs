using System.Text.Json;
using System.Text.Json.Nodes;

namespace EventHorizon.Configuration;

public interface IUserConfigurationFileService
{
    string FilePath { get; }

    void EnsureExists();

    void Save(AppOptions options);
}

public sealed class UserConfigurationFileService : IUserConfigurationFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public UserConfigurationFileService(IPathEnvironment pathEnvironment)
    {
        FilePath = GetDefaultFilePath(pathEnvironment);
        _bundledDefaultFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    public string FilePath { get; }

    private readonly string _bundledDefaultFilePath;

    public void EnsureExists()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        if (!File.Exists(FilePath))
        {
            var initialContent = File.Exists(_bundledDefaultFilePath)
                ? File.ReadAllText(_bundledDefaultFilePath)
                : "{}";
            SafeWrite(initialContent.TrimEnd() + Environment.NewLine);
        }
    }

    public void Save(AppOptions options)
    {
        EnsureExists();

        var root = ReadRoot();
        root.Remove(nameof(AppOptions.CurrentProvider));

        var optionsToPersist = CloneForPersistence(options);
        var persisted = JsonSerializer.SerializeToNode(optionsToPersist, EventHorizonJsonContext.Default.AppOptions)?.AsObject() ?? [];
        foreach (var pair in persisted)
        {
            root[pair.Key] = pair.Value?.DeepClone();
        }

        SafeWrite(root.ToJsonString(JsonOptions) + Environment.NewLine);
    }

    public static string GetDefaultFilePath(IPathEnvironment pathEnvironment)
        => Path.Combine(pathEnvironment.HomeDirectory, ".eventhorizon", "appsettings.json");

    private JsonObject ReadRoot()
    {
        var content = File.ReadAllText(FilePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        try
        {
            return JsonNode.Parse(content)?.AsObject() ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"The configuration file '{FilePath}' is not valid JSON.", ex);
        }
    }

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

    private static AppOptions CloneForPersistence(AppOptions options)
        => new()
        {
            AgUi = options.AgUi,
            Agent = options.Agent,
            Provider = options.Provider,
            CurrentDefaultProvider = options.CurrentDefaultProvider,
            Providers = options.Providers.ToDictionary(
                static pair => pair.Key,
                static pair => CloneProvider(pair.Value),
                StringComparer.OrdinalIgnoreCase),
            Pricing = options.Pricing,
            Conversation = options.Conversation,
            McpServers = options.McpServers.Select(CloneMcpServer).ToList(),
            Skills = new SkillCatalogOptions
            {
                StoragePath = options.Skills.StoragePath,
                Imported = options.Skills.Imported
                    .Select(static item => new ImportedSkillOptions
                    {
                        Name = item.Name,
                        Path = item.Path,
                        Description = item.Description,
                        ImportedAt = item.ImportedAt,
                    })
                    .ToList(),
            },
            CurrentProvider = null,
        };

    private static ProviderOptions CloneProvider(ProviderOptions provider)
        => new()
        {
            Name = provider.Name,
            Type = provider.Type,
            Model = provider.Model,
            Models = [.. provider.Models],
            ApiKey = provider.ApiKey,
            Endpoint = provider.Endpoint,
            Deployment = provider.Deployment,
            UseDefaultAzureCredential = provider.UseDefaultAzureCredential,
        };

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

