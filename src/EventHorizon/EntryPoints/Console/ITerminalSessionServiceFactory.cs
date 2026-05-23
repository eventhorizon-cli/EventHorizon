using EventHorizon.Configuration;
using EventHorizon.Providers;
using EventHorizon.Terminal.Session;

namespace EventHorizon.EntryPoints.Console;

public interface ITerminalSessionServiceFactory
{
    ITerminalSessionService Create(IEventHorizonRuntime runtime, AppOptions options);
}
