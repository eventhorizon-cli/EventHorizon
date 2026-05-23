using EventHorizon.Configuration;
using EventHorizon.Pricing;

namespace EventHorizon.Terminal.Surfaces;

public interface ITerminalSurfaceBuilder
{
    string SurfaceId { get; }

    TerminalSurfaceViewModel Build(TerminalSurfaceBuildContext context);
}
