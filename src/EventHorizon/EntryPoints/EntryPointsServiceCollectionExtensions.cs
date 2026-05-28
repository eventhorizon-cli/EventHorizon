using EventHorizon.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.EntryPoints;

public static class EntryPointsServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonEntryPoints(this IServiceCollection services)
    {
        services.AddSingleton<IEventHorizonApplication, EventHorizonApplication>();
        services.AddSingleton<EventHorizonRuntimeHolder>();
        services.AddSingleton<IEventHorizonRuntime, EventHorizonRuntimeWrapper>();
        services.AddHostedService<RuntimeInitializationHostedService>();
        return services;
    }
}

