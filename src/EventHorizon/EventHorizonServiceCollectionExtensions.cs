using EventHorizon.Commands;
using EventHorizon.Configuration;
using EventHorizon.Context;
using EventHorizon.Conversations;
using EventHorizon.Diagnostics;
using EventHorizon.EntryPoints;
using EventHorizon.Execution;
using EventHorizon.Pricing;
using EventHorizon.Prompting;
using EventHorizon.Providers;
using EventHorizon.Terminal;
using EventHorizon.Workspace;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon;

public static class EventHorizonServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizon(this IServiceCollection services, EffectiveCommandOptions commandOptions, IPathEnvironment pathEnvironment)
    {
        services
            .AddEventHorizonConfiguration(commandOptions, pathEnvironment)
            .AddEventHorizonDiagnostics()
            .AddEventHorizonWorkspace()
            .AddEventHorizonContext()
            .AddEventHorizonConversations()
            .AddEventHorizonCommands()
            .AddEventHorizonPrompting()
            .AddEventHorizonProviders()
            .AddEventHorizonPricing()
            .AddEventHorizonExecution()
            .AddEventHorizonTerminal()
            .AddEventHorizonEntryPoints();

        return services;
    }
}

