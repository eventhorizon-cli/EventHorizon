using EventHorizon.EntryPoints;
using EventHorizon.Workspace;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.AGUI;

public static class AGUIServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonAGUI(this IServiceCollection services)
    {
        services.AddSingleton<RunStore>();
        services.AddSingleton<AGUIEventMapper>();
        services.AddSingleton<AGUICodeAgentEventMapper>();
        services.AddSingleton<AGUISessionService>();
        services.AddSingleton<DiffService>();
        services.AddSingleton(serviceProvider =>
            new WorkspaceSnapshotService(serviceProvider.GetRequiredService<WorkspaceService>().WorkspaceRoot));
        services.AddSingleton<RunService>();
        services.AddSingleton<IAGUIServerRunner, AGUIServerRunner>();
        return services;
    }
}

