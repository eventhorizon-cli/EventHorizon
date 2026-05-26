using System.Collections.ObjectModel;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Dialogs;

public class MultiSelectionDialog<T> : Dialog
{
    private readonly Action<global::Terminal.Gui.App.IRunnable> _requestStop;

    public MultiSelectionDialog(string title, IReadOnlyList<T> items, Func<T, string> label, Action<global::Terminal.Gui.App.IRunnable> requestStop)
    {
        _requestStop = requestStop;
        Title = title;
        Width = Dim.Percent(70);
        Height = Dim.Percent(70);

        var source = new ObservableCollection<string>(items.Select(label));
        var list = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            MarkMultiple = true,
            ShowMarks = true,
        };
        list.SetSource(source);
        ListView = list;

        var closeButton = new Button
        {
            X = 1,
            Y = Pos.Bottom(list),
            Text = "Close",
            IsDefault = true,
        };
        closeButton.Accepted += (_, _) => _requestStop(this);

        Add(list, closeButton);
    }

    public ListView ListView { get; }
}

