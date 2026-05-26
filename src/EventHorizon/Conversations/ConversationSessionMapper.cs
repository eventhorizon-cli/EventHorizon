using EventHorizon.Configuration;
using EventHorizon.Terminal;
using EventHorizon.Workspace;

namespace EventHorizon.Conversations;

public sealed class ConversationSessionMapper : IConversationSessionMapper
{
    private readonly WorkspaceContext _workspaceContext;

    public ConversationSessionMapper(WorkspaceContext workspaceContext)
    {
        _workspaceContext = workspaceContext;
    }

    public ConversationSessionDocument MapToDocument(string name, AppOptions options, TerminalState state, string? serializedSession)
    {
        return new ConversationSessionDocument
        {
            Id = state.SessionId,
            Name = name,
            ProviderType = options.Provider.Type ?? string.Empty,
            Model = options.Provider.Model ?? options.Provider.Deployment ?? string.Empty,
            WorkspaceRoot = _workspaceContext.WorkspaceRoot,
            CreatedAt = state.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow,
            SerializedSession = serializedSession,
            ConversationId = state.ConversationId,
            Transcript = state.Messages.Select(static item => new ConversationTranscriptEntry
            {
                Role = item.Role.ToString().ToLowerInvariant(),
                Text = item.Content,
                Timestamp = item.CreatedAt,
            }).ToList(),
            Usage = new ConversationUsageSnapshot
            {
                InputTokens = 0,
                OutputTokens = 0,
                TotalTokens = state.TotalTokens ?? 0,
                TotalCost = state.LastCostUsd ?? 0m,
                HasPrice = state.LastCostUsd.HasValue,
            }
        };
    }

    public TerminalState MapToState(ConversationSessionDocument document)
    {
        TerminalState state = new()
        {
            SessionId = document.Id,
            CreatedAt = document.CreatedAt,
            ConversationId = document.ConversationId,
            TotalTokens = (int?)document.Usage.TotalTokens,
            LastCostUsd = document.Usage.HasPrice ? document.Usage.TotalCost : null,
        };

        foreach (var entry in document.Transcript)
        {
            state.Messages.Add(new Terminal.Models.TerminalChatMessage(ParseRole(entry.Role), entry.Text, entry.Timestamp));
        }

        state.Status = Terminal.Models.TerminalRunStatus.WaitingForInput;
        return state;
    }

    private static Terminal.Models.TerminalMessageRole ParseRole(string role)
        => role.ToLowerInvariant() switch
        {
            "assistant" => Terminal.Models.TerminalMessageRole.Assistant,
            "tool" => Terminal.Models.TerminalMessageRole.Tool,
            "error" => Terminal.Models.TerminalMessageRole.Error,
            "system" => Terminal.Models.TerminalMessageRole.System,
            _ => Terminal.Models.TerminalMessageRole.User,
        };
}

