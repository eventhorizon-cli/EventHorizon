using EventHorizon.Terminal;
using EventHorizon.Terminal.Models;

namespace EventHorizon.Tests.Terminal;

public sealed class TerminalStateTests
{
    [Fact]
    public void UpsertStreamingAssistantMessage_Appends_To_The_Current_Assistant_Message()
    {
        TerminalState state = new()
        {
            IsStreaming = true,
        };

        state.UpsertStreamingAssistantMessage("Hello");
        state.UpsertStreamingAssistantMessage(" world");
        state.CompleteStreamingAssistantMessage("Hello world");

        var message = Assert.Single(state.Messages);
        Assert.Equal(TerminalMessageRole.Assistant, message.Role);
        Assert.Equal("Hello world", message.Content);
        Assert.False(state.IsStreaming);
    }

    [Fact]
    public void AddInputHistory_Deduplicates_Adjacent_Entries()
    {
        TerminalState state = new();

        state.AddInputHistory("refactor terminal");
        state.AddInputHistory("refactor terminal");
        state.AddInputHistory("run tests");

        Assert.Equal(["refactor terminal", "run tests"], state.InputHistory);
    }

    [Fact]
    public void ClearChat_Removes_All_Messages()
    {
        TerminalState state = new();
        state.AddMessage(TerminalMessageRole.User, "hello");
        state.AddMessage(TerminalMessageRole.Assistant, "hi");

        state.ClearChat();

        Assert.Empty(state.Messages);
    }
}

