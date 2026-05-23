using EventHorizon.Pricing;

namespace EventHorizon.Terminal.Panels;

public sealed class InspectorPanelBuilder : ITerminalPanelBuilder
{
    public string PanelId => TerminalPanelCatalog.Inspector;

    public TerminalPanelViewModel Build(TerminalPanelBuildContext context)
    {
        Microsoft.Extensions.AI.UsageDetails usage = context.UsageTracker.TotalUsage;
        UsageCost totalCost = context.UsageTracker.TotalCost.HasPrice || context.State.TotalCost.HasPrice
            ? context.UsageTracker.TotalCost
            : context.State.TotalCost;

        List<string> lines =
        [
            $"Agent      {context.Options.Agent.Name}",
            $"Provider   {context.Options.Provider.Type}",
            $"Model      {context.Model}",
            $"Workspace  {context.Options.WorkspaceRoot}",
            $"Session    {context.State.SessionId}",
            $"Created    {context.State.CreatedAt:yyyy-MM-dd HH:mm:ss}",
            $"Turns      {context.State.Transcript.Count(static message => string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase))}",
            string.Empty,
            "Usage",
            $"Input      {usage.InputTokenCount ?? usage.InputTextTokenCount ?? context.State.TotalUsage.InputTokenCount ?? context.State.TotalUsage.InputTextTokenCount ?? 0}",
            $"Output     {usage.OutputTokenCount ?? usage.OutputTextTokenCount ?? context.State.TotalUsage.OutputTokenCount ?? context.State.TotalUsage.OutputTextTokenCount ?? 0}",
            $"Total      {usage.TotalTokenCount ?? context.State.TotalUsage.TotalTokenCount ?? 0}",
            totalCost.HasPrice ? $"Cost       {totalCost.TotalCost:F6} USD" : "Cost       unavailable",
            string.Empty,
            "Recent sessions"
        ];

        if (context.State.SavedSessions.Count == 0)
        {
            lines.Add("(none loaded)");
        }
        else
        {
            lines.AddRange(context.State.SavedSessions.Select(static session => $"{session.UpdatedAt:MM-dd HH:mm}  {session.Id}  {session.Name}"));
        }

        if (!string.IsNullOrWhiteSpace(context.State.LastPrompt))
        {
            lines.Add(string.Empty);
            lines.Add("Last prompt");
            lines.Add(context.State.LastPrompt);
        }

        return new TerminalPanelViewModel
        {
            PanelId = PanelId,
            Title = "Inspector",
            IsActive = string.Equals(context.State.ActivePanelId, PanelId, StringComparison.OrdinalIgnoreCase),
            Lines = lines
        };
    }
}