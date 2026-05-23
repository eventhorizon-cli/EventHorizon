using EventHorizon.Pricing;
using EventHorizon.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Execution;

public static class ExecutionServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonExecution(this IServiceCollection services)
    {
        services.AddSingleton<QueryEngine>();
        return services;
    }
}

