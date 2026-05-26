using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Views;

public sealed class ActivityIndicatorView : FrameView
{
    private readonly Label _label;

    public ActivityIndicatorView()
    {
        Title = "Activity";
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
        _label.Text = state.IsStreaming ? "● Thinking" : state.IsToolRunning ? "● Tool running" : "○ Ready";
    }
}

