using EventHorizon.Configuration;
using EventHorizon.Conversations;
using EventHorizon.Terminal;
using EventHorizon.Terminal.Models;
using EventHorizon.Workspace;

namespace EventHorizon.Tests.Conversations;

public sealed class ConversationSessionMapperTests
{
    [Fact]
    public void MapToDocument_And_BackToState_RoundTrips_Core_Conversation_Data()
    {
        ConversationSessionMapper mapper = new(new WorkspaceContext("/tmp/workspace"));
        AppOptions options = new()
        {
            Provider = new ProviderOptions
            {
                Type = "anthropic",
                Model = "claude-sonnet-4"
            }
        };

        TerminalState state = new()
        {
            SessionId = "session-123",
            CreatedAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero),
            ConversationId = "conversation-abc",
            LastCostUsd = 0.03m,
            TotalTokens = 15,
        };
        state.AddMessage(TerminalMessageRole.User, "inspect the repo");
        state.AddMessage(TerminalMessageRole.Assistant, "here is the summary");

        var document = mapper.MapToDocument("demo", options, state, serializedSession: null);
        var restored = mapper.MapToState(document);

        Assert.Equal("session-123", document.Id);
        Assert.Equal("demo", document.Name);
        Assert.Equal("anthropic", document.ProviderType);
        Assert.Equal("claude-sonnet-4", document.Model);
        Assert.Equal("/tmp/workspace", document.WorkspaceRoot);
        Assert.Equal(2, document.Transcript.Count);
        Assert.Equal(15, document.Usage.TotalTokens);

        Assert.Equal(state.SessionId, restored.SessionId);
        Assert.Equal(state.CreatedAt, restored.CreatedAt);
        Assert.Equal(state.ConversationId, restored.ConversationId);
        Assert.Equal(2, restored.Messages.Count);
        Assert.Equal(15, restored.TotalTokens);
        Assert.Equal(0.03m, restored.LastCostUsd);
        Assert.Equal(TerminalRunStatus.WaitingForInput, restored.Status);
    }
}

