using System.Collections.ObjectModel;
using EventHorizon.Terminal.Models;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Views;

public sealed class ToolCallsView : FrameView
{
    private readonly ObservableCollection<string> _items = [];
    private readonly ListView _listView;

    public ToolCallsView()
    {
        Title = "Tools";
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
        foreach (var tool in state.ToolCalls.TakeLast(12))
        {
            _items.Add($"{Glyph(tool.Status)} {tool.Name} {tool.ArgumentsSummary}".Trim());
        }
    }

    private static char Glyph(TerminalToolCallStatus status)
        => status switch
        {
            TerminalToolCallStatus.Succeeded => '✓',
            TerminalToolCallStatus.Running => '●',
            TerminalToolCallStatus.Failed => '✗',
            TerminalToolCallStatus.Cancelled => '⊘',
            _ => '○',
        };
}

