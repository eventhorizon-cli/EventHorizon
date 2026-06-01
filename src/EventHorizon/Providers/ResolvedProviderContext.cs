using EventHorizon.Configuration;

namespace EventHorizon.Providers;

public sealed record ResolvedProviderContext(
    string? ProviderName,
    string ProviderType,
    string Model,
    ProviderOptions Provider);

