using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Context;

public static class ContextServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonContext(this IServiceCollection services)
    {
        services.AddSingleton<ISessionContextBuilder, SessionContextBuilder>();
        return services;
    }
}

