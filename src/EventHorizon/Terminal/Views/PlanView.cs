using System.Collections.ObjectModel;
using EventHorizon.Terminal.Models;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Views;

public sealed class PlanView : FrameView
{
    private readonly ObservableCollection<string> _items = [];
    private readonly ListView _listView;

    public PlanView()
    {
        Title = "Plan";
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
        foreach (var item in state.Plan)
        {
            _items.Add($"{Glyph(item.Status)} {item.Title}".Trim());
        }
    }

    private static char Glyph(TerminalPlanItemStatus status)
        => status switch
        {
            TerminalPlanItemStatus.Completed => '✓',
            TerminalPlanItemStatus.InProgress => '●',
            TerminalPlanItemStatus.Failed => '✗',
            TerminalPlanItemStatus.Skipped => '⊘',
            _ => '○',
        };
}

