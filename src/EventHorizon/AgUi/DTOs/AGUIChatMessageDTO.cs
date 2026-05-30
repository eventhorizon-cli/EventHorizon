namespace EventHorizon.AGUI.DTOs;

public sealed record AGUIChatMessageDTO(
    string Id,
    string SessionId,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    string? Status = null);
