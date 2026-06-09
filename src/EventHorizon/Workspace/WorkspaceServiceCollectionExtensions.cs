using EventHorizon.Workspace.Diff;
using EventHorizon.Workspace.Skills;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Workspace;

public static class WorkspaceServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonWorkspace(this IServiceCollection services)
    {
        services.AddSingleton<IFileStateTrackerAccessor, FileStateTrackerAccessor>();
        services.AddSingleton<IWorkspaceContextAccessor, WorkspaceContextAccessor>();
        services.AddSingleton<IFileSnapshotService, FileSnapshotService>();
        services.AddSingleton<IDiffService, DiffService>();
        services.AddSingleton<IWorkspaceService, WorkspaceService>();
        services.AddScoped<SessionWorkspaceContextFilter>();
        return services;
    }
}
