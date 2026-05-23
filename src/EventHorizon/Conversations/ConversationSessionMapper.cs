using EventHorizon.Configuration;
using EventHorizon.Pricing;
using EventHorizon.Terminal;

namespace EventHorizon.Conversations;

public sealed class ConversationSessionMapper : IConversationSessionMapper
{
    public ConversationSessionDocument MapToDocument(string name, AppOptions options, TerminalConversationState state, string? serializedSession)
    {
        return new ConversationSessionDocument
        {
            Id = state.SessionId,
            Name = name,
            ProviderType = options.Provider.Type,
            Model = options.Provider.Model ?? options.Provider.Deployment ?? string.Empty,
            WorkspaceRoot = options.WorkspaceRoot,
            CreatedAt = state.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow,
            SerializedSession = serializedSession,
            ConversationId = state.ConversationId,
            Transcript = state.Transcript.Select(static item => new ConversationTranscriptEntry
            {
                Role = item.Role,
                Text = item.Text,
                Timestamp = item.Timestamp,
            }).ToList(),
            Usage = new ConversationUsageSnapshot
            {
                InputTokens = state.TotalUsage.InputTokenCount ?? state.TotalUsage.InputTextTokenCount ?? 0,
                OutputTokens = state.TotalUsage.OutputTokenCount ?? state.TotalUsage.OutputTextTokenCount ?? 0,
                TotalTokens = state.TotalUsage.TotalTokenCount ?? ((state.TotalUsage.InputTokenCount ?? 0) + (state.TotalUsage.OutputTokenCount ?? 0)),
                TotalCost = state.TotalCost.TotalCost,
                HasPrice = state.TotalCost.HasPrice,
            }
        };
    }

    public TerminalConversationState MapToState(ConversationSessionDocument document)
    {
        TerminalConversationState state = new()
        {
            SessionId = document.Id,
            CreatedAt = document.CreatedAt,
            ConversationId = document.ConversationId,
            TotalCost = new UsageCost(0, 0, 0, 0, 0, 0, document.Usage.TotalCost, document.Usage.HasPrice, "USD")
        };

        foreach (ConversationTranscriptEntry entry in document.Transcript)
        {
            state.Transcript.Add(new TerminalMessage
            {
                Role = entry.Role,
                Text = entry.Text,
                Timestamp = entry.Timestamp,
            });
        }

        state.TotalUsage.InputTokenCount = document.Usage.InputTokens;
        state.TotalUsage.OutputTokenCount = document.Usage.OutputTokens;
        state.TotalUsage.TotalTokenCount = document.Usage.TotalTokens;
        state.DismissLaunchpad();
        return state;
    }
}

