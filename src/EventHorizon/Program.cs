using EventHorizon.Cli;
using EventHorizon.Configuration;
using EventHorizon.Diagnostics;
using EventHorizon.EntryPoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventHorizon;

public static class Program
{
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
            var parser = new EventHorizonCli();
            if (args.Any(static arg => string.Equals(arg, "--help", StringComparison.Ordinal) ||
                                       string.Equals(arg, "-h", StringComparison.Ordinal)))
            {
                Console.WriteLine(parser.GetHelpText());
                return 0;
            }

            var command = parser.Parse(args);
            host = EventHorizonHost.Create(args, command);
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
            return await host.Services.GetRequiredService<IEventHorizonApplication>().RunAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            Console.WriteLine("Operation canceled.");
            return 0;
        }
        catch (Exception ex)
        {
            IRunErrorLogWriter errorLogWriter =
                host?.Services.GetService<IRunErrorLogWriter>() ?? CreateFallbackErrorLogWriter();
            errorLogWriter.Write("startup", ex, new Dictionary<string, string?> { ["args"] = string.Join(' ', args), });

            Console.Error.WriteLine($"Startup failed. See log: {errorLogWriter.LogFilePath}");
            Console.Error.WriteLine(ex.Message);
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
        => new EventHorizonCli().Parse(args);

    private static IRunErrorLogWriter CreateFallbackErrorLogWriter()
        => new RunErrorLogWriter(new PathEnvironment());

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
