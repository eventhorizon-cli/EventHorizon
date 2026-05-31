using EventHorizon.Engine.Events;
using EventHorizon.Engine.Runs;
using EventHorizon.Engine.Sessions;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Engine;

public static class EngineServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonEngine(this IServiceCollection services)
    {
        services.AddSingleton<RunStore>();
        services.AddSingleton<EventMapper>();
        services.AddSingleton<CodeAgentEventMapper>();
        services.AddSingleton<ISessionContextBuilder, SessionContextBuilder>();
        services.AddSingleton<ISessionStore, FileSessionStore>();
        services.AddSingleton<ISessionSerializer, ChatClientSessionSerializer>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<ISessionModelService, SessionModelService>();
        services.AddSingleton<ISessionTitleGenerator, SessionTitleGenerator>();
        services.AddSingleton<IRunService, RunService>();
        return services;
    }
}
