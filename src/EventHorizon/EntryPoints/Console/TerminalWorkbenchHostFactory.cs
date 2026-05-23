using EventHorizon.Configuration;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Terminal;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.EntryPoints.Console;

public sealed class TerminalWorkbenchHostFactory : ITerminalWorkbenchHostFactory
{
    private readonly IServiceProvider _services;

    public TerminalWorkbenchHostFactory(IServiceProvider services)
    {
        _services = services;
    }

    public TerminalWorkbenchHost Create(IEventHorizonRuntime runtime, AppOptions options, ModelPriceCatalog catalog)
        => ActivatorUtilities.CreateInstance<TerminalWorkbenchHost>(_services, runtime, options, catalog);
}
