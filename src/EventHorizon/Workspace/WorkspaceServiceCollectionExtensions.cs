using EventHorizon.Workspace.Diff;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Workspace;

public static class WorkspaceServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonWorkspace(this IServiceCollection services)
    {
        services.AddSingleton<BackgroundTerminalCommandStore>();
        services.AddSingleton<IFileStateTrackerAccessor, FileStateTrackerAccessor>();
        services.AddSingleton(new ShellCommandRunner());
        services.AddSingleton(serviceProvider =>
        {
            var pathEnvironment = serviceProvider.GetRequiredService<Configuration.IPathEnvironment>();
            return new WorkspaceContext(pathEnvironment.CurrentDirectory);
        });
        services.AddSingleton<IFileSnapshotService>(serviceProvider =>
            new FileSnapshotService(serviceProvider.GetRequiredService<WorkspaceContext>()));
        services.AddSingleton<IDiffService, DiffService>();
        services.AddSingleton<IWorkspaceService>(serviceProvider =>
        {
            var workspaceContext = serviceProvider.GetRequiredService<WorkspaceContext>();
            return new WorkspaceService(
                workspaceContext,
                serviceProvider.GetRequiredService<ShellCommandRunner>(),
                serviceProvider.GetRequiredService<IFileSnapshotService>(),
                serviceProvider.GetRequiredService<IFileStateTrackerAccessor>(),
                serviceProvider.GetRequiredService<BackgroundTerminalCommandStore>());
        });
        services.AddSingleton<WorkspaceSkill>();
        return services;
    }
}
