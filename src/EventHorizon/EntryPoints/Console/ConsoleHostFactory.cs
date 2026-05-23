using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Terminal;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.EntryPoints.Console;

public sealed class ConsoleHostFactory : IConsoleHostFactory
{
    private readonly IServiceProvider _services;

    public ConsoleHostFactory(IServiceProvider services)
    {
        _services = services;
    }

    public ConsoleHost Create(IEventHorizonRuntime runtime, ModelPriceCatalog catalog)
        => ActivatorUtilities.CreateInstance<ConsoleHost>(_services, runtime, catalog);
}
