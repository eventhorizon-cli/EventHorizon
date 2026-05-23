using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventHorizon.Configuration;

public static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonConfiguration(this IServiceCollection services, EffectiveCommandOptions commandOptions, IPathEnvironment pathEnvironment)
    {
        services.AddSingleton(commandOptions);
        services.AddSingleton(pathEnvironment);
        services.AddOptions<AppOptions>().BindConfiguration(string.Empty);
        services.AddSingleton<IPostConfigureOptions<AppOptions>, AppOptionsPostConfigure>();
        services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value);
        return services;
    }
}

