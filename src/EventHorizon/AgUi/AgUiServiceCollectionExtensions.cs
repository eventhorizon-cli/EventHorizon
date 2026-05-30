using System.Text.Json.Serialization;
using EventHorizon.Diff;
using EventHorizon.EntryPoints;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.AGUI;

public static class AGUIServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonAGUI(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(jsonOptions =>
        {
            jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        services.AddSingleton<RunStore>();
        services.AddSingleton<AGUIEventMapper>();
        services.AddSingleton<AGUICodeAgentEventMapper>();
        services.AddSingleton<IAGUISessionService, AGUISessionService>();
        services.AddSingleton<IConversationModelService, ConversationModelService>();
        services.AddSingleton<ISessionTitleGenerator, SessionTitleGenerator>();
        services.AddSingleton<DiffService>();
        services.AddSingleton<RunService>();
        services.AddSingleton<IAGUIServerRunner, AGUIServerRunner>();
        return services;
    }
}
