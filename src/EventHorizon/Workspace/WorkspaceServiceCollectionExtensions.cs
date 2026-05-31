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
        services.AddSingleton<IWorkspaceContextAccessor, WorkspaceContextAccessor>();
        services.AddSingleton<IFileSnapshotService, FileSnapshotService>();
        services.AddSingleton<IDiffService, DiffService>();
        services.AddSingleton<IWorkspaceService, WorkspaceService>();
        services.AddScoped<SessionWorkspaceContextFilter>();
        services.AddSingleton<WorkspaceSkill>();
        return services;
    }
}
