using System.Collections.ObjectModel;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Dialogs;

public class SelectionDialog<T> : Dialog
{
    private readonly Action<global::Terminal.Gui.App.IRunnable> _requestStop;
    private readonly IReadOnlyList<T> _items;
    private readonly Func<T, string> _label;
    private readonly ObservableCollection<string> _displayItems;
    private readonly ListView _listView;

    public SelectionDialog(string title, IReadOnlyList<T> items, Func<T, string> label, Action<global::Terminal.Gui.App.IRunnable> requestStop)
    {
        _requestStop = requestStop;
        _items = items;
        _label = label;
        _displayItems = new ObservableCollection<string>(items.Select(label));

        Title = title;
        Width = Dim.Percent(60);
        Height = Dim.Percent(60);

        _listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };
        _listView.SetSource(_displayItems);

        var selectButton = new Button
        {
            X = 1,
            Y = Pos.Bottom(_listView),
            Text = "Select",
            IsDefault = true,
        };
        selectButton.Accepted += (_, _) => AcceptSelection();

        var cancelButton = new Button
        {
            X = Pos.Right(selectButton) + 2,
            Y = Pos.Top(selectButton),
            Text = "Cancel",
        };
        cancelButton.Accepted += (_, _) =>
        {
            Result = 0;
            _requestStop(this);
        };

        _listView.KeyDown += (_, key) =>
        {
            if (key == Key.Enter)
            {
                AcceptSelection();
                key.Handled = true;
            }
            else if (key == TerminalKeyBindings.Escape)
            {
                Result = 0;
                _requestStop(this);
                key.Handled = true;
            }
        };

        Add(_listView, selectButton, cancelButton);
    }

    public T? SelectedItem { get; private set; }

    private void AcceptSelection()
    {
        if (_listView.SelectedItem is not { } selectedIndex || selectedIndex < 0 || selectedIndex >= _items.Count)
        {
            return;
        }

        SelectedItem = _items[selectedIndex];
        Result = 1;
        _requestStop(this);
    }
}

