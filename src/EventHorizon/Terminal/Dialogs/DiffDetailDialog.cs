using EventHorizon.Terminal.Models;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Dialogs;

public sealed class DiffDetailDialog : Dialog
{
    private readonly Action<global::Terminal.Gui.App.IRunnable> _requestStop;

    public DiffDetailDialog(TerminalDiffItem diff, Action<global::Terminal.Gui.App.IRunnable> requestStop)
    {
        _requestStop = requestStop;
        Title = $"Diff: {diff.Path}";
        Width = Dim.Percent(80);
        Height = Dim.Percent(75);

        var text = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            Text = diff.UnifiedDiff ?? diff.Summary ?? diff.Path,
        };

        var closeButton = new Button
        {
            X = 1,
            Y = Pos.Bottom(text),
            Text = "Close",
            IsDefault = true,
        };
        closeButton.Accepted += (_, _) => _requestStop(this);

        Add(text, closeButton);
    }
}

