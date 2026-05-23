using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Pricing;

public static class PricingServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonPricing(this IServiceCollection services)
    {
        services.AddHttpClient<IModelPriceCatalogService, ModelPriceCatalogService>();
        services.AddSingleton<ISessionUsageTracker, SessionUsageTracker>();
        return services;
    }
}

