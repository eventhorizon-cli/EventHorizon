using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace EventHorizon.Configuration;

public static class ConfigurationManagerExtensions
{
    public static void AddEventHorizonFiles(this ConfigurationManager configuration, IPathEnvironment pathEnvironment)
    {
        var userConfigFilePath = UserConfigurationFileService.GetDefaultFilePath(pathEnvironment);
        var userProvidersFilePath = UserProvidersFileService.GetDefaultFilePath(pathEnvironment);
        var userMcpFilePath = UserMcpFileService.GetDefaultFilePath(pathEnvironment);
        var userSkillsFilePath = UserSkillsFileService.GetDefaultFilePath(pathEnvironment);

        EnsureUserConfigExists(userConfigFilePath);
        EnsureJsonFileExists(userProvidersFilePath);
        EnsureJsonFileExists(userMcpFilePath);
        EnsureJsonFileExists(userSkillsFilePath);

        configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: true);
        configuration.AddJsonFile(userConfigFilePath, optional: false, reloadOnChange: true);
        configuration.AddJsonFile(userProvidersFilePath, optional: true, reloadOnChange: true);
        configuration.AddJsonFile(userMcpFilePath, optional: true, reloadOnChange: true);
        configuration.AddJsonFile(userSkillsFilePath, optional: true, reloadOnChange: true);
    }

    private static void EnsureUserConfigExists(string userConfigFilePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(userConfigFilePath)!);
        if (File.Exists(userConfigFilePath))
        {
            return;
        }

        var bundledConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        File.WriteAllText(userConfigFilePath, CreateUserConfigTemplate(bundledConfigPath));
    }

    private static void EnsureJsonFileExists(string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "{}" + Environment.NewLine);
        }
    }

    private static string CreateUserConfigTemplate(string bundledConfigPath)
    {
        if (!File.Exists(bundledConfigPath))
        {
            return "{}" + Environment.NewLine;
        }

        var initialContent = File.ReadAllText(bundledConfigPath);
        if (string.IsNullOrWhiteSpace(initialContent))
        {
            return "{}" + Environment.NewLine;
        }

        try
        {
            var root = JsonNode.Parse(initialContent)?.AsObject() ?? [];

            root.Remove("Providers");
            root.Remove("McpServers");
            root.Remove("Skills");
            root.Remove("Session");

            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"The bundled configuration template '{bundledConfigPath}' is not valid JSON.", ex);
        }
    }
}
