using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Dialogs;

public sealed class ConfirmDialog : Dialog
{
    private readonly Action<global::Terminal.Gui.App.IRunnable> _requestStop;

    public ConfirmDialog(string title, string message, Action<global::Terminal.Gui.App.IRunnable> requestStop)
    {
        _requestStop = requestStop;
        Title = title;
        Width = Dim.Percent(60);
        Height = 8;

        var label = new Label
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = 2,
            Text = message,
        };

        var confirmButton = new Button
        {
            X = 1,
            Y = Pos.Bottom(label) + 1,
            Text = "OK",
            IsDefault = true,
        };
        confirmButton.Accepted += (_, _) =>
        {
            Result = 1;
            _requestStop(this);
        };

        var cancelButton = new Button
        {
            X = Pos.Right(confirmButton) + 2,
            Y = Pos.Top(confirmButton),
            Text = "Cancel",
        };
        cancelButton.Accepted += (_, _) =>
        {
            Result = 0;
            _requestStop(this);
        };

        KeyDown += (_, key) =>
        {
            if (key == TerminalKeyBindings.Escape)
            {
                Result = 0;
                _requestStop(this);
                key.Handled = true;
            }
        };

        Add(label, confirmButton, cancelButton);
    }
}

