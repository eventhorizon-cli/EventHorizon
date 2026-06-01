using System.Text.Json;
using System.Text.Json.Nodes;

namespace EventHorizon.Configuration;

public sealed class UserSkillsFileService : IUserSkillsFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly IPathEnvironment _pathEnvironment;

    public UserSkillsFileService(IPathEnvironment pathEnvironment)
    {
        _pathEnvironment = pathEnvironment;
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

    public void Save(SkillsOptions options)
    {
        EnsureExists();

        var persisted = new JsonObject
        {
            ["Skills"] = new JsonObject
            {
                [nameof(SkillsOptions.StoragePath)] = options.StoragePath ?? GetDefaultStoragePath(),
                [nameof(SkillsOptions.Imported)] = JsonSerializer.SerializeToNode(
                    options.Imported
                        .Select(static item => new ImportedSkillOptions
                        {
                            Enabled = item.Enabled,
                            Name = item.Name,
                            Path = item.Path,
                            Description = item.Description,
                            ImportedAt = item.ImportedAt,
                        })
                        .ToList(),
                    EventHorizonJsonContext.Default.ListImportedSkillOptions),
            },
        };

        SafeWrite(persisted.ToJsonString(JsonOptions) + Environment.NewLine);
    }

    public static string GetDefaultFilePath(IPathEnvironment pathEnvironment)
        => Path.Combine(pathEnvironment.HomeDirectory, ".eventhorizon", "skills.json");

    private string GetDefaultStoragePath()
        => Path.Combine(_pathEnvironment.HomeDirectory, ".eventhorizon", "skills");

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
