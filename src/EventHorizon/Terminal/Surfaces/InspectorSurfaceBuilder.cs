using EventHorizon.Configuration;
using EventHorizon.Pricing;
using Microsoft.Extensions.AI;

namespace EventHorizon.Terminal.Surfaces;

public sealed class InspectorSurfaceBuilder : ITerminalSurfaceBuilder
{
    public string SurfaceId => "inspector";

    public TerminalSurfaceViewModel Build(TerminalSurfaceBuildContext context)
    {
        UsageDetails usage = context.UsageTracker.TotalUsage;
        UsageCost totalCost = context.UsageTracker.TotalCost.HasPrice ? context.UsageTracker.TotalCost : context.State.TotalCost;
        List<string> lines =
        [
            $"Agent      {context.Options.Agent.Name}",
            $"Provider   {context.Options.Provider.Type}",
            $"Model      {context.Model}",
            $"Sidebar    {TerminalCommandCatalog.GetSidebarModeLabel(context.State.SidebarMode)}",
            $"Session    {context.State.SessionId}",
            $"Turns      {context.State.Transcript.Count(static message => string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase))}",
            string.Empty,
            "Usage",
            $"Input      {usage.InputTokenCount ?? usage.InputTextTokenCount ?? context.State.TotalUsage.InputTokenCount ?? context.State.TotalUsage.InputTextTokenCount ?? 0}",
            $"Output     {usage.OutputTokenCount ?? usage.OutputTextTokenCount ?? context.State.TotalUsage.OutputTokenCount ?? context.State.TotalUsage.OutputTextTokenCount ?? 0}",
            $"Total      {usage.TotalTokenCount ?? context.State.TotalUsage.TotalTokenCount ?? 0}",
            totalCost.HasPrice ? $"Cost       {totalCost.TotalCost:F6} USD" : "Cost       unavailable",
            string.Empty,
            context.State.CommandPalette.IsOpen ? "Palette     open" : "Palette     closed",
            string.IsNullOrWhiteSpace(context.State.FocusedPath) ? "Focus       workspace root" : $"Focus       {context.State.FocusedPath}",
        ];

        if (!string.IsNullOrWhiteSpace(context.State.LastPrompt))
        {
            lines.Add(string.Empty);
            lines.Add("Last prompt");
            lines.Add(context.State.LastPrompt);
        }

        return new TerminalSurfaceViewModel
        {
            SurfaceId = SurfaceId,
            IsVisible = true,
            Panel = new TerminalPanelViewModel
            {
                PanelId = TerminalPanelCatalog.Inspector,
                Title = "Session Inspector",
                IsActive = string.Equals(context.State.ActivePanelId, TerminalPanelCatalog.Inspector, StringComparison.OrdinalIgnoreCase),
                Lines = lines,
            },
        };
    }
}