using EventHorizon.Conversations;

namespace EventHorizon.Terminal.Dialogs;

public sealed class SessionSelectionDialog : SelectionDialog<ConversationSessionSummary>
{
    public SessionSelectionDialog(IReadOnlyList<ConversationSessionSummary> sessions, Action<global::Terminal.Gui.App.IRunnable> requestStop)
        : base("Sessions", sessions, static session => $"{session.Name} ({session.Model})", requestStop)
    {
    }
}

