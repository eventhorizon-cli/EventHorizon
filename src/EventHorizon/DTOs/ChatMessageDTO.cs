namespace EventHorizon.DTOs;

public sealed record ChatMessageDTO(
    string Id,
    string SessionId,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    string? Status = null);
