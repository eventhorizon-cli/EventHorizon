using EventHorizon.Configuration;
using EventHorizon.Pricing;

namespace EventHorizon.Terminal.Panels;

public sealed class TerminalPanelBuildContext
{
    public required AppOptions Options { get; init; }

    public required TerminalConversationState State { get; init; }

    public required SessionUsageTracker UsageTracker { get; init; }

    public required IReadOnlyList<TerminalMessage> Transcript { get; init; }

    public required string Model { get; init; }
}

