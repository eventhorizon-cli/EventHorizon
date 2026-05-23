using EventHorizon.Conversations;
using EventHorizon.Pricing;
using Microsoft.Extensions.AI;

namespace EventHorizon.Terminal;

public sealed class TerminalConversationState
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ConversationId { get; set; }
    public List<TerminalMessage> Transcript { get; } = [];
    public List<TerminalActivityEntry> ActivityFeed { get; } = [];
    public List<TerminalErrorEntry> ErrorFeed { get; } = [];
    public List<TerminalCommandEntry> CommandHistory { get; } = [];
    public List<string> InputHistory { get; } = [];
    public List<ConversationSessionSummary> SavedSessions { get; } = [];
    public UsageDetails TotalUsage { get; } = new();
    public UsageCost TotalCost { get; set; }
    public string? FocusedPath { get; private set; }
    public string ActivePanelId { get; private set; } = TerminalPanelCatalog.Conversation;
    public string SidebarMode { get; private set; } = TerminalSidebarModeCatalog.Overview;
    public string LastPrompt { get; private set; } = string.Empty;
    public string LastAssistantPreview { get; private set; } = string.Empty;
    public string PendingInput { get; set; } = string.Empty;
    public int PendingInputCursorIndex { get; set; }
    public string PendingInputMetadata { get; set; } = string.Empty;
    public bool ShowLaunchpad { get; private set; } = true;
    public int ConversationScrollOffset { get; private set; }
    public TerminalCommandPaletteState CommandPalette { get; } = new();

    public void AddMessage(string role, string text)
    {
        Transcript.Add(new TerminalMessage { Role = role, Text = text, Timestamp = DateTimeOffset.UtcNow });

        if (string.Equals(role, "user", StringComparison.OrdinalIgnoreCase))
        {
            LastPrompt = text;
        }

        if (string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase))
        {
            LastAssistantPreview = text;
        }
    }

    public void SetAssistantPreview(string text)
    {
        LastAssistantPreview = text;
    }

    public void ScrollConversation(int offset)
    {
        if (offset == 0)
        {
            return;
        }

        ConversationScrollOffset = Math.Max(0, ConversationScrollOffset + offset);
    }

    public void ResetConversationScroll()
    {
        ConversationScrollOffset = 0;
    }

    public void DismissLaunchpad()
    {
        ShowLaunchpad = false;
    }

    public void ReopenLaunchpad()
    {
        ShowLaunchpad = true;
    }

    public void AddActivity(string kind, string title, string? detail = null)
    {
        ActivityFeed.Add(new TerminalActivityEntry
        {
            Kind = string.IsNullOrWhiteSpace(kind) ? "info" : kind,
            Title = title,
            Detail = detail ?? string.Empty,
            Timestamp = DateTimeOffset.UtcNow,
        });

        Trim(ActivityFeed, 48);
    }

    public void AddError(string title, string message, string exceptionType, string? logFilePath = null)
    {
        ErrorFeed.Add(new TerminalErrorEntry
        {
            Title = title,
            Message = message,
            ExceptionType = exceptionType,
            LogFilePath = logFilePath ?? string.Empty,
            Timestamp = DateTimeOffset.UtcNow,
        });

        Trim(ErrorFeed, 24);
    }

    public void TrackCommand(string commandText)
    {
        CommandHistory.Add(new TerminalCommandEntry
        {
            CommandText = commandText,
            Timestamp = DateTimeOffset.UtcNow,
        });

        Trim(CommandHistory, 20);
    }

    public void TrackInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        if (InputHistory.Count == 0 || !string.Equals(InputHistory[^1], input, StringComparison.Ordinal))
        {
            InputHistory.Add(input);
        }

        Trim(InputHistory, 100);
    }

    public void ReplaceSavedSessions(IEnumerable<ConversationSessionSummary> sessions)
    {
        SavedSessions.Clear();
        SavedSessions.AddRange(sessions.OrderByDescending(static session => session.UpdatedAt).Take(8));
    }

    public void SetFocusedPath(string? focusedPath)
    {
        FocusedPath = string.IsNullOrWhiteSpace(focusedPath) ? null : focusedPath;
    }

    public void SetActivePanel(string panelId)
    {
        if (!TerminalPanelCatalog.IsKnown(panelId))
        {
            return;
        }

        ActivePanelId = panelId;
        SidebarMode = panelId switch
        {
            TerminalPanelCatalog.Explorer => TerminalSidebarModeCatalog.Files,
            TerminalPanelCatalog.Activity => TerminalSidebarModeCatalog.Activity,
            TerminalPanelCatalog.Commands => TerminalSidebarModeCatalog.Commands,
            _ => SidebarMode,
        };
    }

    public void CycleActivePanel(int offset)
    {
        IReadOnlyList<string> orderedPanels = TerminalPanelCatalog.Ordered;
        int currentIndex = orderedPanels
            .Select(static (panelId, index) => new { panelId, index })
            .FirstOrDefault(item => string.Equals(item.panelId, ActivePanelId, StringComparison.OrdinalIgnoreCase))?.index ?? -1;
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int nextIndex = (currentIndex + offset) % orderedPanels.Count;
        if (nextIndex < 0)
        {
            nextIndex += orderedPanels.Count;
        }

        SetActivePanel(orderedPanels[nextIndex]);
    }

    public void SetSidebarMode(string sidebarMode)
    {
        if (!TerminalSidebarModeCatalog.IsKnown(sidebarMode))
        {
            return;
        }

        SidebarMode = sidebarMode;
    }

    public void CycleSidebarMode(int offset)
    {
        IReadOnlyList<string> orderedModes = TerminalSidebarModeCatalog.Ordered;
        int currentIndex = orderedModes
            .Select(static (mode, index) => new { mode, index })
            .FirstOrDefault(item => string.Equals(item.mode, SidebarMode, StringComparison.OrdinalIgnoreCase))?.index ?? -1;
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int nextIndex = (currentIndex + offset) % orderedModes.Count;
        if (nextIndex < 0)
        {
            nextIndex += orderedModes.Count;
        }

        SidebarMode = orderedModes[nextIndex];
    }

    public void OpenCommandPalette(string? initialQuery = null)
        => CommandPalette.Open(initialQuery);

    public void CloseCommandPalette()
        => CommandPalette.Close();

    public void SetCommandPaletteQuery(string query)
        => CommandPalette.SetQuery(query);

    public void MoveCommandPaletteSelection(int offset, int itemCount)
        => CommandPalette.MoveSelection(offset, itemCount);

    public void SetPendingInputState(string buffer, int cursorIndex, string? metadata = null)
    {
        PendingInput = buffer;
        PendingInputCursorIndex = Math.Clamp(cursorIndex, 0, buffer.Length);
        PendingInputMetadata = metadata ?? string.Empty;
    }

    public void ClearActivity()
    {
        ActivityFeed.Clear();
    }

    public void ClearErrors()
    {
        ErrorFeed.Clear();
    }

    private static void Trim<T>(List<T> items, int maxCount)
    {
        if (items.Count <= maxCount)
        {
            return;
        }

        items.RemoveRange(0, items.Count - maxCount);
    }
}

