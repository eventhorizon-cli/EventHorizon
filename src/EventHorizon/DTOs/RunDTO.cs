namespace EventHorizon.DTOs;

public sealed class RunDTO
{
    public required string Id { get; init; }

    public required string ThreadId { get; init; }

    public required string SessionId { get; init; }

    public required string Task { get; init; }

    public string? WorkingDirectory { get; init; }

    public string? ProviderName { get; init; }

    public string? Model { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? DetailedStatus { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string? Error { get; init; }
}
