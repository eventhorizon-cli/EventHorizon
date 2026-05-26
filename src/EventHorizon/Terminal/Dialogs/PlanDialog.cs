using EventHorizon.Terminal.Models;

namespace EventHorizon.Terminal.Dialogs;

public sealed class PlanDialog : MultiSelectionDialog<TerminalPlanItem>
{
    public PlanDialog(IReadOnlyList<TerminalPlanItem> items, Action<global::Terminal.Gui.App.IRunnable> requestStop)
        : base("Plan", items, static item => $"{ToGlyph(item.Status)} {item.Title}", requestStop)
    {
    }

    private static char ToGlyph(TerminalPlanItemStatus status)
        => status switch
        {
            TerminalPlanItemStatus.Completed => '✓',
            TerminalPlanItemStatus.InProgress => '●',
            TerminalPlanItemStatus.Failed => '✗',
            TerminalPlanItemStatus.Skipped => '⊘',
            _ => '○',
        };
}

