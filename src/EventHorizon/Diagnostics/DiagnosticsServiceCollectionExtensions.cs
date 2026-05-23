using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Diagnostics;

public static class DiagnosticsServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonDiagnostics(this IServiceCollection services)
    {
        services.AddSingleton<IRunErrorLogWriter, RunErrorLogWriter>();
        return services;
    }
}

