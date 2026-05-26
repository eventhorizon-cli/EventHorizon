using EventHorizon.Terminal.Models;

namespace EventHorizon.Terminal.Dialogs;

public sealed class ToolCallsDialog : SelectionDialog<TerminalToolCall>
{
    public ToolCallsDialog(IReadOnlyList<TerminalToolCall> tools, Action<global::Terminal.Gui.App.IRunnable> requestStop)
        : base("Tool Calls", tools, static tool => $"{ToGlyph(tool.Status)} {tool.Name} {tool.ArgumentsSummary}".Trim(), requestStop)
    {
    }

    private static char ToGlyph(TerminalToolCallStatus status)
        => status switch
        {
            TerminalToolCallStatus.Succeeded => '✓',
            TerminalToolCallStatus.Running => '●',
            TerminalToolCallStatus.Failed => '✗',
            TerminalToolCallStatus.Cancelled => '⊘',
            _ => '○',
        };
}

