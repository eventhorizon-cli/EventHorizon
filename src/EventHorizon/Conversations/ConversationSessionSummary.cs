namespace EventHorizon.Conversations;

public sealed record ConversationSessionSummary(string Id, string Name, DateTimeOffset UpdatedAt, string ProviderType, string Model);