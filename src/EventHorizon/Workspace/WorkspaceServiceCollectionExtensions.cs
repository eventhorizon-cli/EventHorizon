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
            var commandOptions = serviceProvider.GetRequiredService<Configuration.EffectiveCommandOptions>();
            var pathEnvironment = serviceProvider.GetRequiredService<Configuration.IPathEnvironment>();
            var workspaceRoot = string.IsNullOrWhiteSpace(commandOptions.WorkspaceRoot)
                ? pathEnvironment.CurrentDirectory
                : commandOptions.WorkspaceRoot;

            return new WorkspaceContext(workspaceRoot);
        });
        services.AddSingleton(serviceProvider =>
        {
            var workspaceContext = serviceProvider.GetRequiredService<WorkspaceContext>();
            return new WorkspaceService(
                workspaceContext.WorkspaceRoot,
                serviceProvider.GetRequiredService<ShellCommandRunner>(),
                serviceProvider.GetRequiredService<BackgroundTerminalCommandStore>());
        });
        services.AddSingleton<WorkspaceSkill>();
        return services;
    }
}

