using System.Text.Json;
using System.Text.Json.Nodes;

namespace EventHorizon.Configuration;

public interface IUserConfigurationFileService
{
    string FilePath { get; }

    void EnsureExists();

    void SaveCurrentProvider(string providerName);
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
    }

    public string FilePath { get; }

    public void EnsureExists()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        if (!File.Exists(FilePath))
        {
            File.WriteAllText(FilePath, "{}" + Environment.NewLine);
        }
    }

    public void SaveCurrentProvider(string providerName)
    {
        EnsureExists();

        var root = ReadRoot();
        root["CurrentProvider"] = providerName;
        File.WriteAllText(FilePath, root.ToJsonString(JsonOptions) + Environment.NewLine);
    }

    public static string GetDefaultFilePath(IPathEnvironment pathEnvironment)
        => Path.Combine(pathEnvironment.HomeDirectory, ".config", "eventhorizon.json");

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
}

