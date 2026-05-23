using EventHorizon.Configuration;
using EventHorizon.Pricing;

namespace EventHorizon.Terminal.Surfaces;

public sealed class TerminalSurfaceBuildContext
{
    public required AppOptions Options { get; init; }
    public required TerminalConversationState State { get; init; }
    public required ISessionUsageTracker UsageTracker { get; init; }
    public required IReadOnlyList<TerminalMessage> Transcript { get; init; }
    public required IReadOnlyList<TerminalPaletteItem> PaletteItems { get; init; }
    public required string Model { get; init; }
    public required bool IsStreaming { get; init; }
    public required int AnimationFrameIndex { get; init; }
}
