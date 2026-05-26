using System.Collections.ObjectModel;
using EventHorizon.Terminal.Models;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Views;

public sealed class ContextFilesView : FrameView
{
    private readonly ObservableCollection<string> _items = [];
    private readonly ListView _listView;

    public ContextFilesView()
    {
        Title = "Context";
        _listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        _listView.SetSource(_items);
        Add(_listView);
    }

    public void Update(TerminalState state)
    {
        _items.Clear();
        foreach (var item in state.ContextFiles.TakeLast(16))
        {
            _items.Add($"{(item.IsSelected ? '●' : '○')} {item.Path}".Trim());
        }
    }
}

