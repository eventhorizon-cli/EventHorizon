using EventHorizon.Configuration;
using EventHorizon.Pricing;
using EventHorizon.Terminal.Session;
using Microsoft.Extensions.Logging;

namespace EventHorizon.Terminal;

public sealed class TerminalRuntimeContext
{
    public TerminalRuntimeContext(AppOptions options, ISessionUsageTracker usageTracker, ITerminalSessionService sessionService, ILogger logger)
    {
        Options = options;
        UsageTracker = usageTracker;
        SessionService = sessionService;
        Logger = logger;
    }

    public AppOptions Options { get; }

    public ISessionUsageTracker UsageTracker { get; }

    public ITerminalSessionService SessionService { get; }

    public ILogger Logger { get; }

    public TerminalConversationState State { get; private set; } = new();

    public bool NeedsTranscriptReplay { get; set; }

    public void RecordError(string title, Exception exception, string? detail = null, bool switchToErrors = true)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Logger.LogError(exception, "[{Title}] {Detail}", title, detail);

        string message = string.IsNullOrWhiteSpace(detail) ? exception.Message : $"{detail} {exception.Message}";
        State.AddError(title, message.Trim(), exception.GetType().Name, null);
        State.AddActivity("error", title, message.Trim());
        if (switchToErrors)
        {
            State.SetSidebarMode(TerminalSidebarModeCatalog.Errors);
        }
    }

    public void ReplaceState(TerminalConversationState state)
    {
        State = state;
    }

    public async Task RefreshSavedSessionsAsync(CancellationToken cancellationToken)
    {
        var sessions = await SessionService.ListAsync(cancellationToken).ConfigureAwait(false);
        State.ReplaceSavedSessions(sessions);
    }
}

