using EventHorizon.Configuration;
using EventHorizon.EntryPoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace EventHorizon;

public static class Program
{
    private sealed class StartupLogger { }

    public static Task<int> Main(string[] args)
        => RunAsync(args);

    internal static async Task<int> RunAsync(string[] args)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;
        IHost? host = null;

        try
        {
            if (args.Any(static arg => string.Equals(arg, "--help", StringComparison.Ordinal) ||
                                       string.Equals(arg, "-h", StringComparison.Ordinal)))
            {
                Console.WriteLine(GetHelpText());
                return 0;
            }

            EnsureNoArguments(args);
            host = BuildHost(args, new PathEnvironment());
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
            await host.Services.GetRequiredService<IEventHorizonApplication>().RunAsync(cancellationToken)
                .ConfigureAwait(false);

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            Console.WriteLine("Operation canceled.");
            return 0;
        }
        catch (Exception ex)
        {
            var logger = host?.Services.GetService<ILogger<StartupLogger>>();
            logger?.LogError(ex, "Startup failed. Args: {Args}", string.Join(' ', args));

            await Console.Error.WriteLineAsync("Startup failed. See logs for details.");
            await Console.Error.WriteLineAsync(ex.Message);
            return 1;
        }
        finally
        {
            if (host is not null)
            {
                await StopHostAsync(host).ConfigureAwait(false);
            }

            Console.CancelKeyPress -= cancelHandler;
        }
    }

    internal static void EnsureNoArguments(string[] args)
    {
        if (args.Length > 0)
        {
            throw new InvalidOperationException($"Unsupported arguments: {string.Join(' ', args)}.");
        }
    }

    private static string GetHelpText()
        => "Usage: EventHorizon\nStarts the AGUI server.";

    internal static IHost BuildHost(string[] args, IPathEnvironment pathEnvironment)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureConfiguration(builder.Configuration, pathEnvironment);
        ConfigureLogging(builder, pathEnvironment);

        var startup = new Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services);

        var app = builder.Build();
        startup.Configure(app);
        return app;
    }

    private static async Task StopHostAsync(IHost host)
    {
        try
        {
            await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            host.Dispose();
        }
    }

    private static void ConfigureConfiguration(ConfigurationManager configuration, IPathEnvironment pathEnvironment)
    {
        var userConfigFilePath = UserConfigurationFileService.GetDefaultFilePath(pathEnvironment);
        var userProvidersFilePath = UserProvidersFileService.GetDefaultFilePath(pathEnvironment);
        EnsureUserConfigExists(userConfigFilePath);
        EnsureUserProvidersConfigExists(userProvidersFilePath);

        configuration.SetBasePath(pathEnvironment.CurrentDirectory);
        configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: false);
        configuration.AddJsonFile(userConfigFilePath, optional: false, reloadOnChange: false);
        configuration.AddJsonFile(userProvidersFilePath, optional: false, reloadOnChange: false);

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

            root.Remove(nameof(AppOptions.CurrentDefaultProvider));
            root.Remove(nameof(AppOptions.CurrentProvider));
            root.Remove(nameof(AppOptions.Provider));
            root[nameof(AppOptions.Providers)] = new System.Text.Json.Nodes.JsonObject();

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
            [nameof(AppOptions.Providers)] = new System.Text.Json.Nodes.JsonObject(),
        }.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, }) + Environment.NewLine;
}
