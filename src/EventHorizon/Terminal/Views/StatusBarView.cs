using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Views;

public sealed class StatusBarView : FrameView
{
    private readonly Label _label;

    public StatusBarView()
    {
        Title = "Status";
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
        var statusText = state.Status switch
        {
            Models.TerminalRunStatus.Idle => "Idle",
            Models.TerminalRunStatus.Thinking => "Thinking",
            Models.TerminalRunStatus.Streaming => "Streaming",
            Models.TerminalRunStatus.ToolRunning => "ToolRunning",
            Models.TerminalRunStatus.WaitingForInput => "WaitingForInput",
            Models.TerminalRunStatus.Cancelled => "Cancelled",
            Models.TerminalRunStatus.Error => "Error",
            _ => state.Status.ToString(),
        };

        _label.Text = $"● {statusText} │ {state.CurrentModel ?? "-"} │ {state.GitBranch ?? "-"} │ {state.CurrentWorkingDirectory ?? "."} │ Ctrl+P Commands │ Ctrl+C Cancel";
    }
}

