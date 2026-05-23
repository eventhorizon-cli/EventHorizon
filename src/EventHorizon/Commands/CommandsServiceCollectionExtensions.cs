using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Commands;

public static class CommandsServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonCommands(this IServiceCollection services)
    {
        services.AddSingleton<ISlashCommandService, SlashCommandCatalog>();
        return services;
    }
}

