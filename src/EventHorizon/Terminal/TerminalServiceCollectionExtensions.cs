using EventHorizon.Terminal.Commands;
using EventHorizon.Terminal.Dialogs;
using EventHorizon.Terminal.Layout;
using EventHorizon.Terminal.Session;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Terminal;

public static class TerminalServiceCollectionExtensions
{
    public static IServiceCollection AddEventHorizonTerminal(this IServiceCollection services)
    {
        services.AddSingleton<TerminalState>();
        services.AddSingleton<TerminalGuiHost>();
        services.AddSingleton<TerminalLayoutManager>();
        services.AddSingleton<TerminalResizeObserver>();
        services.AddSingleton<ITerminalAgentAdapter, TerminalAgentAdapter>();
        services.AddSingleton<ITerminalSessionService, TerminalSessionService>();
        services.AddSingleton<TerminalEventDispatcher>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<TerminalCommandRegistry>();
        services.AddSingleton<TerminalApp>();

        services.AddSingleton<ITerminalCommand, HelpCommand>();
        services.AddSingleton<ITerminalCommand, ClearCommand>();
        services.AddSingleton<ITerminalCommand, ExitCommand>();
        services.AddSingleton<ITerminalCommand, CancelCommand>();
        services.AddSingleton<ITerminalCommand, ModelCommand>();
        services.AddSingleton<ITerminalCommand, SessionCommand>();
        services.AddSingleton<ITerminalCommand, FilesCommand>();
        services.AddSingleton<ITerminalCommand, ToolsCommand>();
        services.AddSingleton<ITerminalCommand, DiffCommand>();
        services.AddSingleton<ITerminalCommand, PlanCommand>();
        services.AddSingleton<ITerminalCommand, LayoutCommand>();
        services.AddSingleton<ITerminalCommand, ThemeCommand>();
        return services;
    }
}

