namespace EventHorizon.Providers;

public sealed class ChatRequestOverrides
{
    public string? ProviderName { get; init; }

    public string? Model { get; init; }

    public static ChatRequestOverrides Empty { get; } = new();
}

