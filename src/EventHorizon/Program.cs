using EventHorizon.Configuration;
using EventHorizon.EntryPoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

            var command = ParseArguments(args);
            host = EventHorizonHost.Create(args, command);
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

    internal static EffectiveCommandOptions ParseArguments(string[] args)
    {
        string? configFile = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!string.Equals(argument, "--config", StringComparison.Ordinal) &&
                !string.Equals(argument, "-c", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unsupported argument '{argument}'. Only --config is supported.");
            }

            if (index == args.Length - 1)
            {
                throw new InvalidOperationException("Missing value for --config.");
            }

            configFile = args[++index];
        }

        return new EffectiveCommandOptions
        {
            Command = EffectiveCommandOptions.StartupMode,
            ConfigFile = configFile,
        };
    }

    private static string GetHelpText()
        => "Usage: EventHorizon [--config <path>]\nStarts the AGUI server.\nOptions: --config|-c";

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
}
