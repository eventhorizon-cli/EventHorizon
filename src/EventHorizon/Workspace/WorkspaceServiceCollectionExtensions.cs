using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventHorizon.Workspace;

public static class WorkspaceServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonWorkspace(this IServiceCollection services)
    {
        services.AddSingleton<BackgroundTerminalCommandStore>();
        services.AddSingleton(new ShellCommandRunner());
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<Configuration.AppOptions>>().Value;
            return new WorkspaceService(
                options.WorkspaceRoot,
                serviceProvider.GetRequiredService<ShellCommandRunner>(),
                serviceProvider.GetRequiredService<BackgroundTerminalCommandStore>());
        });
        services.AddSingleton<WorkspaceSkill>();
        return services;
    }
}

