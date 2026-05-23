using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Prompting;

public static class PromptingServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonPrompting(this IServiceCollection services)
    {
        services.AddSingleton<ISystemPromptFactory, SystemPromptFactory>();
        services.AddSingleton<ICodingInstructionsBuilder, CodingInstructionsBuilder>();
        return services;
    }
}

