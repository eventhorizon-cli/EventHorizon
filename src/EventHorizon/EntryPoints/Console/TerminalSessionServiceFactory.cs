using EventHorizon.Configuration;
using EventHorizon.Conversations;
using EventHorizon.Providers;
using EventHorizon.Terminal.Session;

namespace EventHorizon.EntryPoints.Console;

public sealed class TerminalSessionServiceFactory : ITerminalSessionServiceFactory
{
    private readonly IConversationSessionStore _sessionStore;
    private readonly IConversationSessionSerializer _sessionSerializer;
    private readonly IConversationSessionMapper _sessionMapper;

    public TerminalSessionServiceFactory(IConversationSessionStore sessionStore, IConversationSessionSerializer sessionSerializer, IConversationSessionMapper sessionMapper)
    {
        _sessionStore = sessionStore;
        _sessionSerializer = sessionSerializer;
        _sessionMapper = sessionMapper;
    }

    public ITerminalSessionService Create(IEventHorizonRuntime runtime, AppOptions options)
        => new TerminalSessionService(runtime.Agent, options, _sessionStore, _sessionSerializer, _sessionMapper);
}
