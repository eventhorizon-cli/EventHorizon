using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Dialogs;

public sealed class TextInputDialog : Dialog
{
    private readonly Action<global::Terminal.Gui.App.IRunnable> _requestStop;

    public TextInputDialog(string title, string prompt, string initialValue, Action<global::Terminal.Gui.App.IRunnable> requestStop)
    {
        _requestStop = requestStop;
        Title = title;
        Width = Dim.Percent(60);
        Height = 10;

        var label = new Label
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Text = prompt,
        };

        Input = new TextField
        {
            X = 1,
            Y = Pos.Bottom(label),
            Width = Dim.Fill(2),
            Text = initialValue,
        };

        var okButton = new Button
        {
            X = 1,
            Y = Pos.Bottom(Input) + 1,
            Text = "OK",
            IsDefault = true,
        };
        okButton.Accepted += (_, _) =>
        {
            Result = 1;
            _requestStop(this);
        };

        var cancelButton = new Button
        {
            X = Pos.Right(okButton) + 2,
            Y = Pos.Top(okButton),
            Text = "Cancel",
        };
        cancelButton.Accepted += (_, _) =>
        {
            Result = 0;
            _requestStop(this);
        };

        Add(label, Input, okButton, cancelButton);
    }

    public TextField Input { get; }
}

