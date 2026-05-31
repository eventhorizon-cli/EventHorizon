using System.Text.Json;
using System.Text.Json.Nodes;

namespace EventHorizon.Configuration;

public sealed class UserProvidersFileService : IUserProvidersFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public UserProvidersFileService(IPathEnvironment pathEnvironment)
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

    public void Save(ProvidersOptions options)
    {
        EnsureExists();

        var persisted = new JsonObject
        {
            ["Providers"] = new JsonObject
            {
                [nameof(ProvidersOptions.CurrentDefaultProvider)] = options.CurrentDefaultProvider,
                [nameof(ProvidersOptions.Providers)] = JsonSerializer.SerializeToNode(
                    options.Providers.ToDictionary(
                        static pair => pair.Key,
                        static pair => CloneProvider(pair.Value),
                        StringComparer.OrdinalIgnoreCase),
                    EventHorizonJsonContext.Default.DictionaryStringProviderOptions),
            },
        };

        SafeWrite(persisted.ToJsonString(JsonOptions) + Environment.NewLine);
    }

    public static string GetDefaultFilePath(IPathEnvironment pathEnvironment)
        => Path.Combine(pathEnvironment.HomeDirectory, ".eventhorizon", "providers.json");

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
}
