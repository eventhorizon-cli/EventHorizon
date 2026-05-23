using EventHorizon.EntryPoints.Console;
using EventHorizon.Terminal.Commands;
using EventHorizon.Terminal.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Terminal;

public static class TerminalServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonTerminal(this IServiceCollection services)
    {
        services.AddSingleton<ITerminalLayoutRenderer, ConsoleTerminalLayoutRenderer>();
        services.AddSingleton<ITerminalWindowSizeMonitor, ConsoleTerminalWindowSizeMonitor>();
        services.AddSingleton<TerminalWorkbenchComposer>();
        services.AddSingleton<TerminalCommandDispatcher>();
        services.AddSingleton<ITerminalSurfaceBuilder, LaunchpadSurfaceBuilder>();
        services.AddSingleton<ITerminalSurfaceBuilder, SidebarSurfaceBuilder>();
        services.AddSingleton<ITerminalSurfaceBuilder, MainSurfaceBuilder>();
        services.AddSingleton<ITerminalSurfaceBuilder, InspectorSurfaceBuilder>();
        services.AddSingleton<ITerminalSurfaceBuilder, DockSurfaceBuilder>();
        services.AddSingleton<ITerminalSurfaceBuilder, CommandPaletteOverlayBuilder>();
        services.AddSingleton<ITerminalCommandHandler, HelpCommandHandler>();
        services.AddSingleton<ITerminalCommandHandler, StatsCommandHandler>();
        services.AddSingleton<ITerminalCommandHandler, SaveSessionCommandHandler>();
        services.AddSingleton<ITerminalCommandHandler, SessionsCommandHandler>();
        services.AddSingleton<ITerminalCommandHandler, RestoreSessionCommandHandler>();
        services.AddSingleton<ITerminalCommandHandler, FocusCommandHandler>();
        services.AddSingleton<ITerminalCommandHandler, SidebarCommandHandler>();
        services.AddSingleton<ITerminalCommandHandler, ClearActivityCommandHandler>();
        services.AddSingleton<ITerminalCommandHandler, ResetSessionCommandHandler>();
        services.AddSingleton<ITerminalCommandHandler, ExitCommandHandler>();
        services.AddSingleton<ISessionUsageTrackerFactory, SessionUsageTrackerFactory>();
        services.AddSingleton<IQueryEngineFactory, QueryEngineFactory>();
        services.AddSingleton<ITerminalSessionServiceFactory, TerminalSessionServiceFactory>();
        services.AddSingleton<ITerminalRuntimeContextFactory, TerminalRuntimeContextFactory>();
        services.AddSingleton<ITerminalInputControllerFactory, TerminalInputControllerFactory>();
        return services;
    }
}

