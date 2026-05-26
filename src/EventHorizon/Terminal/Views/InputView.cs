using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Views;

public sealed class InputView : FrameView
{
    private const string DefaultHintText = "Type a command or message and press Enter to send";

    private const string FooterText = "Enter send · Ctrl+P commands · Ctrl+H help · Ctrl+C cancel";

    private readonly Label _hintLabel;
    private readonly TextField _textField;
    private int _historyIndex = -1;

    public InputView()
    {
        Title = "Prompt";
        Height = 6;
        SchemeName = "Dialog";

        var promptLabel = new Label
        {
            X = 1,
            Y = 0,
            Width = 2,
            Height = 1,
            Text = ">",
            SchemeName = "TopLevel",
        };

        _hintLabel = new Label
        {
            X = Pos.Right(promptLabel) + 1,
            Y = 0,
            Width = Dim.Fill(4),
            Height = 1,
            Text = DefaultHintText,
        };

        _textField = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = 1,
            Text = string.Empty,
            SchemeName = "TopLevel",
        };
        _textField.ValueChanged += (_, args) =>
        {
            if (SuperView?.Data is TerminalState state)
            {
                state.CurrentInput = args.NewValue ?? string.Empty;
            }
        };
        _textField.Accepted += (_, _) => SubmitCurrentInput();
        KeyDown += OnShortcutKeyDown;

        var footerLabel = new Label
        {
            X = 1,
            Y = Pos.Bottom(_textField) + 1,
            Width = Dim.Fill(2),
            Height = 1,
            Text = FooterText,
            SchemeName = "Menu",
        };

        Add(promptLabel, _hintLabel, _textField, footerLabel);
    }

    public event Func<string, Task>? Submitted;

    public event Func<Key, Task>? ShortcutPressed;

    public void Update(TerminalState state)
    {
        if (!string.Equals(_textField.Text, state.CurrentInput, StringComparison.Ordinal))
        {
            _textField.Text = state.CurrentInput;
        }
    }

    public bool FocusInput() => _textField.SetFocus();

    public void SetInput(string text)
    {
        _textField.Text = text;
    }

    private void OnShortcutKeyDown(object? sender, Key key)
    {
        if (key == TerminalKeyBindings.InsertNewLine)
        {
            _hintLabel.Text = "Single-line prompt mode is active · press Enter to send";
            key.Handled = true;
            return;
        }

        if (key == Key.CursorUp.WithCtrl)
        {
            NavigateHistory(-1);
            key.Handled = true;
            return;
        }

        if (key == Key.CursorDown.WithCtrl)
        {
            NavigateHistory(1);
            key.Handled = true;
            return;
        }

        if (key == TerminalKeyBindings.CancelOrExit
            || key == TerminalKeyBindings.CommandPalette
            || key == TerminalKeyBindings.Help
            || key == TerminalKeyBindings.Tools
            || key == TerminalKeyBindings.Files
            || key == TerminalKeyBindings.Clear
            || key == TerminalKeyBindings.Exit
            || key == TerminalKeyBindings.Refresh)
        {
            if (ShortcutPressed is not null)
            {
                _ = ShortcutPressed.Invoke(key);
            }

            key.Handled = true;
        }
    }

    private void SubmitCurrentInput()
    {
        if (Submitted is null)
        {
            return;
        }

        var text = _textField.Text;
        _textField.Text = string.Empty;
        _historyIndex = -1;
        if (SuperView?.Data is TerminalState state)
        {
            state.CurrentInput = string.Empty;
        }

        _ = Submitted.Invoke(text.TrimEnd());
    }

    private void NavigateHistory(int offset)
    {
        if (SuperView?.Data is not TerminalState state || state.InputHistory.Count == 0)
        {
            return;
        }

        _historyIndex = Math.Clamp(_historyIndex + offset, -1, state.InputHistory.Count - 1);
        _textField.Text = _historyIndex >= 0 ? state.InputHistory[_historyIndex] : string.Empty;
        state.CurrentInput = _textField.Text;
        _hintLabel.Text = DefaultHintText;
    }
}
