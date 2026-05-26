using EventHorizon.Terminal.Models;

namespace EventHorizon.Terminal.Dialogs;

public sealed class ContextFilesDialog : MultiSelectionDialog<TerminalContextFile>
{
    public ContextFilesDialog(IReadOnlyList<TerminalContextFile> files, Action<global::Terminal.Gui.App.IRunnable> requestStop)
        : base("Context Files", files, static file => $"{(file.IsSelected ? '●' : '○')} {file.Path}", requestStop)
    {
    }
}

