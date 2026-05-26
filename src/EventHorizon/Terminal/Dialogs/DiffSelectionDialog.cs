using EventHorizon.Terminal.Models;

namespace EventHorizon.Terminal.Dialogs;

public sealed class DiffSelectionDialog : SelectionDialog<TerminalDiffItem>
{
    public DiffSelectionDialog(IReadOnlyList<TerminalDiffItem> diffs, Action<global::Terminal.Gui.App.IRunnable> requestStop)
        : base("Diffs", diffs, static diff => $"{ToGlyph(diff.Kind)} {diff.Path} {diff.Summary}".Trim(), requestStop)
    {
    }

    private static char ToGlyph(TerminalDiffKind kind)
        => kind switch
        {
            TerminalDiffKind.Added => 'A',
            TerminalDiffKind.Deleted => 'D',
            TerminalDiffKind.Renamed => 'R',
            _ => 'M',
        };
}

