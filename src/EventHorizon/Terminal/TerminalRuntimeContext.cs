using EventHorizon.Configuration;
using EventHorizon.Diagnostics;
using EventHorizon.Pricing;
using EventHorizon.Terminal.Session;

namespace EventHorizon.Terminal;

public sealed class TerminalRuntimeContext
{
    public TerminalRuntimeContext(AppOptions options, ISessionUsageTracker usageTracker, ITerminalSessionService sessionService, IRunErrorLogWriter errorLogWriter)
    {
        Options = options;
        UsageTracker = usageTracker;
        SessionService = sessionService;
        ErrorLogWriter = errorLogWriter;
    }

    public AppOptions Options { get; }

    public ISessionUsageTracker UsageTracker { get; }

    public ITerminalSessionService SessionService { get; }

    public IRunErrorLogWriter ErrorLogWriter { get; }

    public TerminalConversationState State { get; private set; } = new();

    public bool NeedsTranscriptReplay { get; set; }

    public void RecordError(string title, Exception exception, string? detail = null, bool switchToErrors = true)
    {
        ArgumentNullException.ThrowIfNull(exception);

        ErrorLogWriter.Write("tui", exception, new Dictionary<string, string?>
        {
            ["workspace"] = Options.WorkspaceRoot,
            ["sessionId"] = State.SessionId,
            ["title"] = title,
        });

        string message = string.IsNullOrWhiteSpace(detail) ? exception.Message : $"{detail} {exception.Message}";
        State.AddError(title, message.Trim(), exception.GetType().Name, ErrorLogWriter.LogFilePath);
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

