namespace EventHorizon.Terminal;

internal readonly record struct TerminalProviderConnectionSummary(
    bool IsReady,
    string ProviderType,
    string ModelName,
    string StatusDetail,
    string AuthenticationDetail,
    string EndpointDisplay,
    IReadOnlyList<string> GuidanceLines);
