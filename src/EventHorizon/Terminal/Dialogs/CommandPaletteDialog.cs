using System.Collections.ObjectModel;
using EventHorizon.Terminal.Commands;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Dialogs;

public sealed class CommandPaletteDialog : Dialog
{
    private readonly Action<global::Terminal.Gui.App.IRunnable> _requestStop;
    private readonly IReadOnlyList<ITerminalCommand> _commands;
    private readonly ObservableCollection<string> _items;
    private readonly ListView _listView;

    public CommandPaletteDialog(IReadOnlyList<ITerminalCommand> commands, Action<global::Terminal.Gui.App.IRunnable> requestStop)
    {
        _requestStop = requestStop;
        _commands = commands;
        FilteredCommands = commands;
        _items = new ObservableCollection<string>(commands.Select(Format));

        Title = "Command Palette";
        Width = Dim.Percent(70);
        Height = Dim.Percent(65);

        SearchField = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Text = "/",
        };
        SearchField.ValueChanged += (_, _) => ApplyFilter();
        SearchField.KeyDown += OnSearchKeyDown;

        _listView = new ListView
        {
            X = 1,
            Y = Pos.Bottom(SearchField) + 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(4),
        };
        _listView.SetSource(_items);
        _listView.KeyDown += OnListKeyDown;

        var footer = new Label
        {
            X = 1,
            Y = Pos.Bottom(_listView),
            Width = Dim.Fill(2),
            Text = "↑↓ Move │ Enter Select │ Esc Cancel",
        };

        Add(SearchField, _listView, footer);
    }

    public TextField SearchField { get; }

    public ITerminalCommand? SelectedCommand { get; private set; }

    private void OnSearchKeyDown(object? sender, Key key)
    {
        if (key == Key.Enter)
        {
            AcceptCurrent();
            key.Handled = true;
            return;
        }

        if (key == TerminalKeyBindings.Escape)
        {
            _requestStop(this);
            key.Handled = true;
        }
    }

    private void OnListKeyDown(object? sender, Key key)
    {
        if (key == Key.Enter)
        {
            AcceptCurrent();
            key.Handled = true;
            return;
        }

        if (key == TerminalKeyBindings.Escape)
        {
            _requestStop(this);
            key.Handled = true;
        }
    }

    private void AcceptCurrent()
    {
        if (_listView.SelectedItem is not { } selectedIndex || selectedIndex < 0 || selectedIndex >= FilteredCommands.Count)
        {
            SelectedCommand = FilteredCommands.FirstOrDefault();
        }
        else
        {
            SelectedCommand = FilteredCommands[selectedIndex];
        }

        if (SelectedCommand is not null)
        {
            Result = 1;
        }

        _requestStop(this);
    }

    private IReadOnlyList<ITerminalCommand> FilteredCommands { get; set; } = [];

    private void ApplyFilter()
    {
        var query = SearchField.Text ?? string.Empty;
        FilteredCommands = _commands
            .Where(command => Format(command).Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _items.Clear();
        foreach (var command in FilteredCommands)
        {
            _items.Add(Format(command));
        }

        if (FilteredCommands.Count == 0)
        {
            SelectedCommand = null;
        }
        else
        {
            _listView.SelectedItem = 0;
        }
    }

    private static string Format(ITerminalCommand command)
        => $"> {command.Name.PadRight(12)} {command.Description}";
}

