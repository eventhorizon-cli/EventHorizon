using EventHorizon.Terminal.Layout;
using EventHorizon.Terminal.Models;

namespace EventHorizon.Terminal;

public sealed class TerminalState
{
    public event EventHandler? Changed;

    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? ConversationId { get; set; }

    public TerminalRunStatus Status { get; set; } = TerminalRunStatus.Idle;

    public List<TerminalChatMessage> Messages { get; } = [];

    public List<TerminalToolCall> ToolCalls { get; } = [];

    public List<TerminalPlanItem> Plan { get; } = [];

    public List<TerminalDiffItem> Diffs { get; } = [];

    public List<TerminalContextFile> ContextFiles { get; } = [];

    public List<string> InputHistory { get; } = [];

    public string CurrentInput { get; set; } = string.Empty;

    public string? CurrentModel { get; set; }

    public string? CurrentSession { get; set; }

    public string? CurrentWorkingDirectory { get; set; }

    public string? GitBranch { get; set; }

    public string? ProviderType { get; set; }

    public string? LastStatusMessage { get; set; }

    public string? ErrorSummary { get; set; }

    public Exception? LastException { get; set; }

    public TerminalLayoutMode? ForcedLayoutMode { get; set; }

    public TerminalLayoutMode ActiveLayoutMode { get; set; } = TerminalLayoutMode.Standard;

    public bool IsStreaming { get; set; }

    public bool IsToolRunning { get; set; }

    public bool IsDirty { get; set; }

    public bool ExitRequested { get; set; }

    public bool HasUnreadError { get; set; }

    public decimal? LastCostUsd { get; set; }

    public int? TotalTokens { get; set; }

    public string CurrentThemeName { get; set; } = TerminalTheme.Midnight.Name;

    public CancellationTokenSource? CurrentRunCancellation { get; set; }

    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.Now;

    public void AddMessage(TerminalMessageRole role, string content)
    {
        Messages.Add(new TerminalChatMessage(role, content, DateTimeOffset.UtcNow));
        Touch();
    }

    public void UpsertStreamingAssistantMessage(string delta)
    {
        var lastMessage = Messages.LastOrDefault();
        if (lastMessage is { Role: TerminalMessageRole.Assistant } && IsStreaming)
        {
            Messages[^1] = lastMessage with { Content = lastMessage.Content + delta };
        }
        else
        {
            Messages.Add(new TerminalChatMessage(TerminalMessageRole.Assistant, delta, DateTimeOffset.UtcNow));
        }

        Touch();
    }

    public void CompleteStreamingAssistantMessage(string completedMessage)
    {
        if (string.IsNullOrWhiteSpace(completedMessage))
        {
            IsStreaming = false;
            Touch();
            return;
        }

        var lastMessage = Messages.LastOrDefault();
        if (lastMessage is { Role: TerminalMessageRole.Assistant } && IsStreaming)
        {
            Messages[^1] = lastMessage with { Content = completedMessage };
        }
        else
        {
            Messages.Add(new TerminalChatMessage(TerminalMessageRole.Assistant, completedMessage, DateTimeOffset.UtcNow));
        }

        IsStreaming = false;
        Touch();
    }

    public void AddInputHistory(string input)
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
        Touch();
    }

    public void ReplacePlan(IEnumerable<TerminalPlanItem> items)
    {
        Plan.Clear();
        Plan.AddRange(items);
        Touch();
    }

    public void ReplaceDiffs(IEnumerable<TerminalDiffItem> items)
    {
        Diffs.Clear();
        Diffs.AddRange(items);
        Touch();
    }

    public void ReplaceContextFiles(IEnumerable<TerminalContextFile> items)
    {
        ContextFiles.Clear();
        ContextFiles.AddRange(items);
        Touch();
    }

    public void ClearChat()
    {
        Messages.Clear();
        Touch();
    }

    public void SetError(string message, Exception? exception = null)
    {
        ErrorSummary = message;
        LastException = exception;
        HasUnreadError = true;
        Status = TerminalRunStatus.Error;
        Touch();
    }

    public void ClearError()
    {
        ErrorSummary = null;
        LastException = null;
        HasUnreadError = false;
        Touch();
    }

    public void Touch()
    {
        IsDirty = true;
        LastUpdatedAt = DateTimeOffset.Now;
        Changed?.Invoke(this, EventArgs.Empty);
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

