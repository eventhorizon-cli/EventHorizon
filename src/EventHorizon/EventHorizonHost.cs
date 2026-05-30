using EventHorizon.Configuration;
using EventHorizon.EntryPoints;
using EventHorizon.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace EventHorizon;

public static class EventHorizonHost
{
    public static IHost Create(string[] args, EffectiveCommandOptions commandOptions)
        => Create(args, commandOptions, new PathEnvironment());

    public static IHost Create(string[] args, EffectiveCommandOptions commandOptions, IPathEnvironment pathEnvironment)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureConfiguration(builder.Configuration, commandOptions, pathEnvironment);
        ConfigureLogging(builder, pathEnvironment);

        builder.Services.AddEventHorizon(commandOptions, pathEnvironment);
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, string.Join(';', GetUrls(builder.Configuration)));

        var app = builder.Build();
        ConfigureWebApplication(app);
        return app;
    }

    private static void ConfigureLogging(WebApplicationBuilder builder, IPathEnvironment pathEnvironment)
    {
        var logDirectory = GetLogDirectory(pathEnvironment);
        Directory.CreateDirectory(logDirectory);

        var logFilePath = Path.Combine(logDirectory, "error.log");

        var logger = new LoggerConfiguration()
            .MinimumLevel.Error()
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Error)
            .CreateLogger();

        Log.Logger = logger;
        builder.Logging.AddSerilog(logger, dispose: true);
    }

    private static string GetLogDirectory(IPathEnvironment pathEnvironment)
    {
        var home = pathEnvironment.HomeDirectory;

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(home, "Library", "Logs", "EventHorizon");
        }

        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "EventHorizon", "Logs");
        }

        // Default to a user-scoped config/log directory on Linux and other Unix-like systems.
        return Path.Combine(home, ".config", "eventhorizon");
    }

    private static void ConfigureConfiguration(
        ConfigurationManager configuration,
        EffectiveCommandOptions commandOptions,
        IPathEnvironment pathEnvironment)
    {
        var userConfigFilePath = UserConfigurationFileService.GetDefaultFilePath(pathEnvironment);
        EnsureUserConfigExists(userConfigFilePath);

        configuration.SetBasePath(pathEnvironment.CurrentDirectory);
        configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: false);
        configuration.AddJsonFile(userConfigFilePath, optional: false, reloadOnChange: false);


        if (!string.IsNullOrWhiteSpace(commandOptions.ConfigFile))
        {
            var externalConfigPath = Path.GetFullPath(commandOptions.ConfigFile);
            if (!File.Exists(externalConfigPath))
            {
                throw new InvalidOperationException($"The configuration file '{externalConfigPath}' does not exist.");
            }

            configuration.AddJsonFile(externalConfigPath, optional: false, reloadOnChange: false);
        }

        configuration.AddEnvironmentVariables(prefix: "EVENTHORIZON__");
    }

    private static string[] GetUrls(IConfiguration configuration)
        => configuration.GetSection("AGUI:Urls").Get<HashSet<string>>()?.ToArray()
            ?? throw new InvalidOperationException("AGUI:Urls configuration is required.");

    private static void ConfigureWebApplication(WebApplication app)
    {
        var runtimeInitializer = app.Services.GetRequiredService<IEventHorizonRuntimeInitializer>();
        runtimeInitializer.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

        app.MapControllers();

        var options = app.Services.GetRequiredService<AppOptions>();
        var runtime = app.Services.GetRequiredService<IEventHorizonRuntime>();
        AGUI.AGUIEndpoints.Map(app, options.AGUI, runtime);
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
        var defaultContent = File.Exists(bundledConfigPath)
            ? File.ReadAllText(bundledConfigPath)
            : "{}";
        File.WriteAllText(userConfigFilePath, defaultContent.TrimEnd() + Environment.NewLine);
    }
}
