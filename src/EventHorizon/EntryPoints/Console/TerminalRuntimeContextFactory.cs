using EventHorizon.Configuration;
using EventHorizon.Diagnostics;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Terminal;

namespace EventHorizon.EntryPoints.Console;

public sealed class TerminalRuntimeContextFactory : ITerminalRuntimeContextFactory
{
    private readonly ITerminalSessionServiceFactory _sessionServiceFactory;
    private readonly IRunErrorLogWriter _errorLogWriter;

    public TerminalRuntimeContextFactory(ITerminalSessionServiceFactory sessionServiceFactory, IRunErrorLogWriter errorLogWriter)
    {
        _sessionServiceFactory = sessionServiceFactory;
        _errorLogWriter = errorLogWriter;
    }

    public TerminalRuntimeContext Create(IEventHorizonRuntime runtime, AppOptions options, SessionUsageTracker usageTracker)
        => new(options, usageTracker, _sessionServiceFactory.Create(runtime, options), _errorLogWriter);
}
