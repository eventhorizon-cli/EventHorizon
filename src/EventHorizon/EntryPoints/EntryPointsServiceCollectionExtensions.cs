using EventHorizon.EntryPoints;
using EventHorizon.EntryPoints.Console;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.EntryPoints;

public static class EntryPointsServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonEntryPoints(this IServiceCollection services)
    {
        services.AddSingleton<IEventHorizonApplication, EventHorizonApplication>();
        services.AddSingleton<IRemoteEventHorizonRuntimeFactory, RemoteEventHorizonRuntimeFactory>();
        services.AddSingleton<IAguiServerRunner, AguiServerRunner>();
        services.AddSingleton<IMcpServerRunner, McpServerRunner>();
        services.AddSingleton<IConsoleHostFactory, ConsoleHostFactory>();
        services.AddSingleton<ITerminalWorkbenchHostFactory, TerminalWorkbenchHostFactory>();
        return services;
    }
}

