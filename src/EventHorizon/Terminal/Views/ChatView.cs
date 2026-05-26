using System.Text;
using EventHorizon.Terminal.Models;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Views;

public sealed class ChatView : FrameView
{
    private readonly TextView _textView;
    private readonly Label _hintLabel;

    public ChatView()
    {
        Title = "Conversation";

        _hintLabel = new Label
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(2),
            Height = 1,
            Text = "Session history · tool output stays above the prompt",
        };

        _textView = new TextView
        {
            X = 0,
            Y = Pos.Bottom(_hintLabel),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        Add(_hintLabel, _textView);
    }

    public void Update(TerminalState state)
    {
        StringBuilder builder = new();
        foreach (var message in state.Messages)
        {
            builder.AppendLine(RoleLabel(message.Role));
            builder.AppendLine("  " + message.Content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\n  ", StringComparison.Ordinal));
            builder.AppendLine();
        }

        if (state.IsStreaming)
        {
            builder.AppendLine("Assistant");
            builder.AppendLine("  ● Streaming...");
        }

        _textView.Text = builder.ToString().TrimEnd();
    }

    private static string RoleLabel(TerminalMessageRole role)
        => role switch
        {
            TerminalMessageRole.User => "You",
            TerminalMessageRole.Assistant => "Assistant",
            TerminalMessageRole.Tool => "Tool",
            TerminalMessageRole.Error => "Error",
            _ => "System",
        };
}

