using EventHorizon.Terminal.Models;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Dialogs;

public sealed class ToolDetailDialog : Dialog
{
    private readonly Action<global::Terminal.Gui.App.IRunnable> _requestStop;

    public ToolDetailDialog(TerminalToolCall tool, Action<global::Terminal.Gui.App.IRunnable> requestStop)
    {
        _requestStop = requestStop;
        Title = $"Tool: {tool.Name}";
        Width = Dim.Percent(75);
        Height = Dim.Percent(70);

        var content = string.Join(
            Environment.NewLine,
            new[]
            {
                $"Name: {tool.Name}",
                $"Status: {tool.Status}",
                $"Arguments: {tool.ArgumentsSummary}",
                string.Empty,
                tool.Output ?? tool.Error ?? tool.Description ?? string.Empty,
            });

        var text = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            Text = content,
        };

        var closeButton = new Button
        {
            X = 1,
            Y = Pos.Bottom(text),
            Text = "Close",
            IsDefault = true,
        };
        closeButton.Accepted += (_, _) => _requestStop(this);

        Add(text, closeButton);
    }
}

