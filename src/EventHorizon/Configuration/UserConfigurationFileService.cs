using System.Text.Json;
using System.Text.Json.Nodes;

namespace EventHorizon.Configuration;

public interface IUserConfigurationFileService
{
    string FilePath { get; }

    void EnsureExists();

    void Save(AppOptions options);

    string CreateInitialContent();
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
            SafeWrite(CreateInitialContent());
        }
    }

    public void Save(AppOptions options)
    {
        EnsureExists();

        var root = ReadRoot();
        root.Remove(nameof(ProvidersOptions.CurrentDefaultProvider));
        root.Remove(nameof(ProvidersOptions.Providers));
        root.Remove(nameof(McpOptions.Servers));
        root.Remove(nameof(SkillsOptions.Imported));
        root.Remove(nameof(SkillsOptions.StoragePath));

        var persisted = JsonSerializer.SerializeToNode(options, EventHorizonJsonContext.Default.AppOptions)?.AsObject() ?? [];
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

    public string CreateInitialContent()
    {
        if (!File.Exists(_bundledDefaultFilePath))
        {
            return "{}" + Environment.NewLine;
        }

        var initialContent = File.ReadAllText(_bundledDefaultFilePath);
        if (string.IsNullOrWhiteSpace(initialContent))
        {
            return "{}" + Environment.NewLine;
        }

        try
        {
            var root = JsonNode.Parse(initialContent)?.AsObject() ?? [];

            root.Remove(nameof(ProvidersOptions.CurrentDefaultProvider));
            root.Remove(nameof(ProvidersOptions.Providers));
            root.Remove(nameof(McpOptions.Servers));
            root.Remove(nameof(SkillsOptions.Imported));
            root.Remove(nameof(SkillsOptions.StoragePath));

            return root.ToJsonString(JsonOptions) + Environment.NewLine;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"The bundled configuration template '{_bundledDefaultFilePath}' is not valid JSON.", ex);
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
}
