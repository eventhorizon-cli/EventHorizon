using EventHorizon.Configuration;
using EventHorizon.Conversations;
using EventHorizon.Pricing;
using EventHorizon.Terminal;

namespace EventHorizon.Tests.Conversations;

public sealed class ConversationSessionMapperTests
{
    [Fact]
    public void MapToDocument_And_BackToState_RoundTrips_Core_Conversation_Data()
    {
        ConversationSessionMapper mapper = new();
        AppOptions options = new()
        {
            WorkspaceRoot = "/tmp/workspace",
            Provider = new ProviderOptions
            {
                Type = "anthropic",
                Model = "claude-sonnet-4"
            }
        };

        TerminalConversationState state = new()
        {
            SessionId = "session-123",
            CreatedAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero),
            ConversationId = "conversation-abc",
            TotalCost = new UsageCost(10, 5, 0, 0.01m, 0.02m, 0m, 0.03m, true, "USD")
        };
        state.AddMessage("user", "inspect the repo");
        state.AddMessage("assistant", "here is the summary");
        state.TotalUsage.InputTokenCount = 10;
        state.TotalUsage.OutputTokenCount = 5;
        state.TotalUsage.TotalTokenCount = 15;

        ConversationSessionDocument document = mapper.MapToDocument("demo", options, state, serializedSession: null);
        TerminalConversationState restored = mapper.MapToState(document);

        Assert.Equal("session-123", document.Id);
        Assert.Equal("demo", document.Name);
        Assert.Equal("anthropic", document.ProviderType);
        Assert.Equal("claude-sonnet-4", document.Model);
        Assert.Equal(2, document.Transcript.Count);
        Assert.Equal(15, document.Usage.TotalTokens);

        Assert.Equal(state.SessionId, restored.SessionId);
        Assert.Equal(state.CreatedAt, restored.CreatedAt);
        Assert.Equal(state.ConversationId, restored.ConversationId);
        Assert.Equal(2, restored.Transcript.Count);
        Assert.Equal(10, restored.TotalUsage.InputTokenCount);
        Assert.Equal(5, restored.TotalUsage.OutputTokenCount);
        Assert.Equal(15, restored.TotalUsage.TotalTokenCount);
        Assert.True(restored.TotalCost.HasPrice);
        Assert.Equal(0.03m, restored.TotalCost.TotalCost);
        Assert.False(restored.ShowLaunchpad);
    }
}

