using EventHorizon.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EventHorizon;

public static class EventHorizonHost
{
    public static IHost Create(string[] args, EffectiveCommandOptions commandOptions)
    {
        IPathEnvironment pathEnvironment = new PathEnvironment();
        var builder = Host.CreateApplicationBuilder(args);

        ConfigureConfiguration(builder.Configuration, commandOptions, pathEnvironment);
        ConfigureLogging(builder, pathEnvironment);

        builder.Services.AddEventHorizon(commandOptions, pathEnvironment);

        return builder.Build();
    }

    private static void ConfigureLogging(HostApplicationBuilder builder, IPathEnvironment pathEnvironment)
    {
        var logDirectory = GetLogDirectory(pathEnvironment);
        Directory.CreateDirectory(logDirectory);

        var logFilePath = Path.Combine(logDirectory, "error.log");

        // Remove default providers to prevent console logging.
        builder.Logging.ClearProviders();

        var logger = new LoggerConfiguration()
            // Only write Error and higher severity logs.
            .MinimumLevel.Error()
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
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
        var defaultConfigDirectory = Path.Combine(pathEnvironment.HomeDirectory, ".config", "eventhorizon");

        configuration.SetBasePath(pathEnvironment.CurrentDirectory);
        configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: false);
        configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        configuration.AddJsonFile("eventhorizon.json", optional: true, reloadOnChange: false);

        if (Directory.Exists(defaultConfigDirectory))
        {
            configuration.AddJsonFile(Path.Combine(defaultConfigDirectory, "appsettings.json"), optional: true, reloadOnChange: false);
            configuration.AddJsonFile(Path.Combine(defaultConfigDirectory, "eventhorizon.json"), optional: true, reloadOnChange: false);
        }

        if (!string.IsNullOrWhiteSpace(commandOptions.ConfigFile))
        {
            configuration.AddJsonFile(Path.GetFullPath(commandOptions.ConfigFile), optional: false, reloadOnChange: false);
        }

        configuration.AddEnvironmentVariables(prefix: "EVENTHORIZON__");
    }
}
