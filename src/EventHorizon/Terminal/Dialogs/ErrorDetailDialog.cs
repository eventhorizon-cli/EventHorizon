using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Dialogs;

public sealed class ErrorDetailDialog : Dialog
{
    private readonly Action<global::Terminal.Gui.App.IRunnable> _requestStop;

    public ErrorDetailDialog(string message, Exception? exception, Action<global::Terminal.Gui.App.IRunnable> requestStop)
    {
        _requestStop = requestStop;
        Title = "Error";
        Width = Dim.Percent(75);
        Height = Dim.Percent(70);

        var text = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            Text = exception is null ? message : $"{message}{Environment.NewLine}{Environment.NewLine}{exception}",
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

