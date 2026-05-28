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

    [Fact]
    public void UpdateCommandSuggestions_Replaces_Items_And_Tracks_Selection()
    {
        TerminalState state = new();

        state.UpdateCommandSuggestions(
            "/mo",
            [
                new TerminalCommandDescriptor("/model", "Switch model"),
                new TerminalCommandDescriptor("/mode", "Example"),
            ],
            1);

        Assert.True(state.CommandSuggestions.IsOpen);
        Assert.Equal("/mo", state.CommandSuggestions.Query);
        Assert.Equal(1, state.CommandSuggestions.SelectedIndex);
        Assert.Equal(["/model", "/mode"], state.CommandSuggestions.Items.Select(static item => item.Name));
    }

    [Fact]
    public void CloseCommandSuggestions_Clears_Suggestion_State()
    {
        TerminalState state = new();
        state.UpdateCommandSuggestions("/", [new TerminalCommandDescriptor("/help", "Show help")]);

        state.CloseCommandSuggestions();

        Assert.False(state.CommandSuggestions.IsOpen);
        Assert.Empty(state.CommandSuggestions.Items);
        Assert.Equal(string.Empty, state.CommandSuggestions.Query);
        Assert.Equal(0, state.CommandSuggestions.SelectedIndex);
    }

    [Fact]
    public void SetModelConnectionStatus_Updates_Model_Connection_Status()
    {
        TerminalState state = new();

        state.SetModelConnectionStatus(ModelConnectionStatus.Connected);

        Assert.Equal(ModelConnectionStatus.Connected, state.ModelConnectionStatus);
    }
}

