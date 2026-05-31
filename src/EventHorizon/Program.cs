using EventHorizon.Configuration;
using Serilog;
using Serilog.Events;

namespace EventHorizon;

public static class Program
{
    public static void Main(string[] args)
    {
        using var host = BuildHost(args, new PathEnvironment());
        host.Run();
    }

    internal static IHost BuildHost(string[] args, IPathEnvironment pathEnvironment)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureConfiguration(builder.Configuration, pathEnvironment);
        ConfigureLogging(builder, pathEnvironment);

        Startup.ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();
        Startup.Configure(app);
        return app;
    }

    private static void ConfigureConfiguration(ConfigurationManager configuration, IPathEnvironment pathEnvironment)
    {
        var userConfigFilePath = UserConfigurationFileService.GetDefaultFilePath(pathEnvironment);
        var userProvidersFilePath = UserProvidersFileService.GetDefaultFilePath(pathEnvironment);
        var userMcpFilePath = UserMcpFileService.GetDefaultFilePath(pathEnvironment);
        var userSkillsFilePath = UserSkillsFileService.GetDefaultFilePath(pathEnvironment);
        EnsureUserConfigExists(userConfigFilePath);
        EnsureUserProvidersConfigExists(userProvidersFilePath);
        EnsureUserMcpConfigExists(userMcpFilePath);
        EnsureUserSkillsConfigExists(userSkillsFilePath);

        configuration.SetBasePath(pathEnvironment.CurrentDirectory);
        configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: true);
        configuration.AddJsonFile(userConfigFilePath, optional: false, reloadOnChange: true);
        configuration.AddJsonFile(userProvidersFilePath, optional: false, reloadOnChange: true);
        configuration.AddJsonFile(userMcpFilePath, optional: false, reloadOnChange: true);
        configuration.AddJsonFile(userSkillsFilePath, optional: false, reloadOnChange: true);

        configuration[nameof(PathEnvironment) + ":CurrentDirectory"] = pathEnvironment.CurrentDirectory;
        configuration[nameof(PathEnvironment) + ":HomeDirectory"] = pathEnvironment.HomeDirectory;
    }

    private static void ConfigureLogging(WebApplicationBuilder builder, IPathEnvironment pathEnvironment)
    {
        var logFilePath = Path.Combine(pathEnvironment.HomeDirectory, ".config", "eventhorizon", "error.log");
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Error()
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Error);

        var logger = loggerConfiguration.CreateLogger();

        Log.Logger = logger;
        builder.Logging.AddSerilog(logger, dispose: true);
    }

    private static void EnsureUserConfigExists(string userConfigFilePath)
    {
        var directory = Path.GetDirectoryName(userConfigFilePath)!;
        Directory.CreateDirectory(directory);
        if (File.Exists(userConfigFilePath))
        {
            return;
        }

        var bundledConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var initialContent = CreateUserConfigTemplate(bundledConfigPath);
        File.WriteAllText(userConfigFilePath, initialContent);
    }

    private static void EnsureUserProvidersConfigExists(string userProvidersFilePath)
    {
        var directory = Path.GetDirectoryName(userProvidersFilePath)!;
        Directory.CreateDirectory(directory);
        if (File.Exists(userProvidersFilePath))
        {
            return;
        }

        File.WriteAllText(userProvidersFilePath, CreateUserProvidersConfigTemplate());
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
            var root = System.Text.Json.Nodes.JsonNode.Parse(initialContent)?.AsObject() ?? [];

            root.Remove(nameof(ProvidersOptions.CurrentDefaultProvider));
            root.Remove(nameof(ProvidersOptions.Providers));
            root.Remove(nameof(McpOptions.Servers));
            root.Remove(nameof(SkillsOptions.Imported));
            root.Remove(nameof(SkillsOptions.StoragePath));
            root.Remove("Session");

            return root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, }) +
                   Environment.NewLine;
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidOperationException(
                $"The bundled configuration template '{bundledConfigPath}' is not valid JSON.", ex);
        }
    }

    private static string CreateUserProvidersConfigTemplate()
        => new System.Text.Json.Nodes.JsonObject
        {
            [nameof(ProvidersOptions.Providers)] = new System.Text.Json.Nodes.JsonObject(),
        }.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, }) + Environment.NewLine;

    private static void EnsureUserMcpConfigExists(string userMcpFilePath)
    {
        var directory = Path.GetDirectoryName(userMcpFilePath)!;
        Directory.CreateDirectory(directory);
        if (File.Exists(userMcpFilePath))
        {
            return;
        }

        File.WriteAllText(userMcpFilePath, CreateUserMcpConfigTemplate());
    }

    private static void EnsureUserSkillsConfigExists(string userSkillsFilePath)
    {
        var directory = Path.GetDirectoryName(userSkillsFilePath)!;
        Directory.CreateDirectory(directory);
        if (File.Exists(userSkillsFilePath))
        {
            return;
        }

        File.WriteAllText(userSkillsFilePath, CreateUserSkillsConfigTemplate());
    }

    private static string CreateUserMcpConfigTemplate()
        => new System.Text.Json.Nodes.JsonObject
        {
            [nameof(McpOptions.Servers)] = new System.Text.Json.Nodes.JsonArray(),
        }.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, }) + Environment.NewLine;

    private static string CreateUserSkillsConfigTemplate()
        => new System.Text.Json.Nodes.JsonObject
        {
            [nameof(SkillsOptions.Imported)] = new System.Text.Json.Nodes.JsonArray(),
        }.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, }) + Environment.NewLine;
}
