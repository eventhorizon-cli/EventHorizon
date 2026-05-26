using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Dialogs;

public sealed class HelpDialog : Dialog
{
    private readonly Action<global::Terminal.Gui.App.IRunnable> _requestStop;

    public HelpDialog(string content, Action<global::Terminal.Gui.App.IRunnable> requestStop)
    {
        _requestStop = requestStop;
        Title = "Help";
        Width = Dim.Percent(70);
        Height = Dim.Percent(75);

        var text = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            Text = content,
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

