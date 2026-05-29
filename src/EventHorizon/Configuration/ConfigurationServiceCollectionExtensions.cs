using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EventHorizon.Configuration;

public static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonConfiguration(this IServiceCollection services, EffectiveCommandOptions commandOptions, IPathEnvironment pathEnvironment)
    {
        services.AddSingleton(commandOptions);
        services.AddSingleton(pathEnvironment);
        services.AddSingleton<IUserConfigurationFileService, UserConfigurationFileService>();
        services.AddOptions<AppOptions>().BindConfiguration(string.Empty);
        services.AddSingleton<IAppOptionsInitializer, AppOptionsInitializer>();
        services.AddSingleton<IPostConfigureOptions<AppOptions>, AppOptionsPostConfigure>();
        services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value);
        services.AddSingleton<IAppConfigurationService, AppConfigurationService>();
        services.AddSingleton<IProviderTestingService, ProviderTestingService>();
        services.AddSingleton<IMcpService, McpService>();
        services.AddSingleton<ISkillService, SkillService>();
        services.AddSingleton<IProviderConfigurationService, ProviderConfigurationService>();
        services.AddSingleton<IHostedService, CurrentProviderSelectionHostedService>();
        return services;
    }
}

