using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Views;

public sealed class HeaderView : FrameView
{
    private readonly Label _label;

    public HeaderView()
    {
        Title = "Header";
        Height = 3;

        _label = new Label
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(2),
            Height = 1,
        };

        Add(_label);
    }

    public void Update(TerminalState state)
    {
        _label.Text = $"EventHorizon  model:{state.CurrentModel ?? "unknown"}  session:{state.CurrentSession ?? "main"}  branch:{state.GitBranch ?? "-"}  tokens:{state.TotalTokens?.ToString() ?? "-"}  cwd:{state.CurrentWorkingDirectory ?? "."}";
    }
}

