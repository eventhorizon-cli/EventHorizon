using EventHorizon.Configuration;
using EventHorizon.Pricing;

namespace EventHorizon.Terminal.Surfaces;

public sealed class LaunchpadSurfaceBuilder : ITerminalSurfaceBuilder
{
    public string SurfaceId => "launchpad";

    public TerminalSurfaceViewModel Build(TerminalSurfaceBuildContext context) => new()
    {
        SurfaceId = SurfaceId,
        IsVisible = context.State.ShowLaunchpad && !context.IsStreaming,
        Panel = TerminalLaunchpad.BuildPanel(context.Options, context.State, context.AnimationFrameIndex),
    };
}