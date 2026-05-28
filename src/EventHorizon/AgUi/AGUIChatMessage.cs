namespace EventHorizon.AGUI;

public sealed record AGUIChatMessage(
    string Id,
    string SessionId,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    string? Status = null);

