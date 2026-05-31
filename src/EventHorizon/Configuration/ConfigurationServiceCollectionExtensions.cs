using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventHorizon.Configuration;

public static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonConfiguration(this IServiceCollection services, IPathEnvironment pathEnvironment)
    {
        services.AddSingleton(pathEnvironment);
        services.AddSingleton<IUserConfigurationFileService, UserConfigurationFileService>();
        services.AddSingleton<IUserProvidersFileService, UserProvidersFileService>();
        services.AddSingleton<IUserMcpFileService, UserMcpFileService>();
        services.AddSingleton<IUserSkillsFileService, UserSkillsFileService>();

        services.AddSingleton<IOptionsNormalizer, OptionsNormalizer>();

        services.AddOptions<AgentOptions>().BindConfiguration("Agent");
        services.AddOptions<PricingOptions>().BindConfiguration("Pricing");
        services.AddOptions<ProvidersOptions>().BindConfiguration("Providers");
        services.AddOptions<McpOptions>().BindConfiguration("McpServers");
        services.AddOptions<SkillsOptions>().BindConfiguration("Skills");

        services.AddSingleton<IAppConfigurationService, AppConfigurationService>();
        services.AddSingleton<IProviderTestingService, ProviderTestingService>();
        services.AddSingleton<IMcpService, McpService>();
        services.AddSingleton<ISkillService, SkillService>();
        services.AddSingleton<IProviderConfigurationService, ProviderConfigurationService>();
        services.AddSingleton<Providers.ISkillProviderFactory, Providers.SkillProviderFactory>();
        services.AddSingleton<IHostedService, CurrentProviderSelectionHostedService>();
        return services;
    }
}
