using System.Collections.ObjectModel;
using EventHorizon.Terminal.Models;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Views;

public sealed class DiffView : FrameView
{
    private readonly ObservableCollection<string> _items = [];
    private readonly ListView _listView;

    public DiffView()
    {
        Title = "Diff";
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
        foreach (var item in state.Diffs.TakeLast(12))
        {
            _items.Add($"{Glyph(item.Kind)} {item.Path} {item.Summary}".Trim());
        }
    }

    private static char Glyph(TerminalDiffKind kind)
        => kind switch
        {
            TerminalDiffKind.Added => 'A',
            TerminalDiffKind.Deleted => 'D',
            TerminalDiffKind.Renamed => 'R',
            _ => 'M',
        };
}

