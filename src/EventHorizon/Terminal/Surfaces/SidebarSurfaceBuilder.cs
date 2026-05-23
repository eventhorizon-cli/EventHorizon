using EventHorizon.Configuration;
using EventHorizon.Pricing;
using Microsoft.Extensions.AI;

namespace EventHorizon.Terminal.Surfaces;

public sealed class SidebarSurfaceBuilder : ITerminalSurfaceBuilder
{
    public string SurfaceId => "sidebar";

    public TerminalSurfaceViewModel Build(TerminalSurfaceBuildContext context)
        => new()
        {
            SurfaceId = SurfaceId,
            IsVisible = !context.State.ShowLaunchpad || context.IsStreaming,
            Panel = context.State.SidebarMode switch
            {
                TerminalSidebarModeCatalog.Files => BuildFilesPanel(context),
                TerminalSidebarModeCatalog.Activity => BuildActivityPanel(context),
                TerminalSidebarModeCatalog.Commands => BuildCommandsPanel(context),
                TerminalSidebarModeCatalog.Sessions => BuildSessionsPanel(context),
                TerminalSidebarModeCatalog.Errors => BuildErrorsPanel(context),
                _ => BuildOverviewPanel(context),
            },
        };

    private static TerminalPanelViewModel BuildOverviewPanel(TerminalSurfaceBuildContext context)
    {
        TerminalActivityEntry? latestActivity = context.State.ActivityFeed.LastOrDefault();
        string focus = string.IsNullOrWhiteSpace(context.State.FocusedPath) ? "workspace root" : context.State.FocusedPath;
        UsageDetails usage = context.UsageTracker.TotalUsage;
        int input = ToInt(usage.InputTokenCount ?? usage.InputTextTokenCount ?? context.State.TotalUsage.InputTokenCount ?? context.State.TotalUsage.InputTextTokenCount);
        int output = ToInt(usage.OutputTokenCount ?? usage.OutputTextTokenCount ?? context.State.TotalUsage.OutputTokenCount ?? context.State.TotalUsage.OutputTextTokenCount);
        int total = ToInt(usage.TotalTokenCount ?? context.State.TotalUsage.TotalTokenCount) is var parsedTotal && parsedTotal > 0
            ? parsedTotal
            : input + output;
        UsageCost totalCost = context.UsageTracker.TotalCost.HasPrice ? context.UsageTracker.TotalCost : context.State.TotalCost;

        List<string> lines =
        [
            $"Mode         {TerminalCommandCatalog.GetSidebarModeLabel(context.State.SidebarMode)}",
            $"Focus        {focus}",
            $"Workspace    {Path.GetFileName(context.Options.WorkspaceRoot)}",
            latestActivity is null ? "Recent       no activity yet" : $"Recent       {latestActivity.Timestamp:HH:mm:ss} [{latestActivity.Kind}] {latestActivity.Title}",
        ];

        if (!string.IsNullOrWhiteSpace(latestActivity?.Detail))
        {
            lines.Add($"             {latestActivity.Detail}");
        }

        lines.Add($"Usage        {input}/{output}/{total}{(totalCost.HasPrice ? $" · {totalCost.TotalCost:F6} USD" : string.Empty)}");
        lines.Add($"Model        {context.Options.Provider.Type}/{context.Model}");
        lines.Add($"Saved        {context.State.SavedSessions.Count}");
        lines.Add($"Errors       {context.State.ErrorFeed.Count}");
        lines.Add($"Prompt       {(string.IsNullOrWhiteSpace(context.State.LastPrompt) ? "waiting for first prompt" : Summarize(context.State.LastPrompt, 48))}");
        lines.Add("Sidebar      /sidebar overview|files|activity|commands|sessions|errors");

        return new TerminalPanelViewModel
        {
            PanelId = "overview",
            Title = "Session Overview",
            IsActive = string.Equals(context.State.ActivePanelId, TerminalPanelCatalog.Conversation, StringComparison.OrdinalIgnoreCase),
            Lines = lines,
        };
    }

    private static TerminalPanelViewModel BuildFilesPanel(TerminalSurfaceBuildContext context)
        => new()
        {
            PanelId = TerminalPanelCatalog.Explorer,
            Title = "Workspace Files",
            IsActive = string.Equals(context.State.ActivePanelId, TerminalPanelCatalog.Explorer, StringComparison.OrdinalIgnoreCase),
            Lines = WorkspaceExplorerSnapshotBuilder.Build(context.Options.WorkspaceRoot, context.State.FocusedPath),
        };

    private static TerminalPanelViewModel BuildActivityPanel(TerminalSurfaceBuildContext context)
    {
        if (context.State.ActivityFeed.Count == 0)
        {
            return new TerminalPanelViewModel
            {
                PanelId = TerminalPanelCatalog.Activity,
                Title = "Activity Timeline",
                IsActive = string.Equals(context.State.ActivePanelId, TerminalPanelCatalog.Activity, StringComparison.OrdinalIgnoreCase),
                Lines = ["No activity yet.", "Prompt execution, saves, restores, tools, and errors will appear here."],
            };
        }

        List<string> lines = [];
        foreach (TerminalActivityEntry entry in context.State.ActivityFeed.OrderByDescending(static item => item.Timestamp).Take(12))
        {
            lines.Add($"{entry.Timestamp:HH:mm:ss}  [{entry.Kind}] {entry.Title}");
            if (!string.IsNullOrWhiteSpace(entry.Detail))
            {
                lines.Add($"  {entry.Detail}");
            }
        }

        return new TerminalPanelViewModel
        {
            PanelId = TerminalPanelCatalog.Activity,
            Title = context.IsStreaming ? "Streaming Activity" : "Activity Timeline",
            IsActive = string.Equals(context.State.ActivePanelId, TerminalPanelCatalog.Activity, StringComparison.OrdinalIgnoreCase),
            Lines = lines,
        };
    }

    private static TerminalPanelViewModel BuildCommandsPanel(TerminalSurfaceBuildContext context)
        => new()
        {
            PanelId = TerminalPanelCatalog.Commands,
            Title = "Command Deck",
            IsActive = string.Equals(context.State.ActivePanelId, TerminalPanelCatalog.Commands, StringComparison.OrdinalIgnoreCase),
            Lines = TerminalCommandCatalog.BuildCommandDeckLines(context.State.CommandHistory, context.State.SavedSessions, context.State.FocusedPath, context.State.SidebarMode),
        };

    private static TerminalPanelViewModel BuildSessionsPanel(TerminalSurfaceBuildContext context)
    {
        List<string> lines =
        [
            $"Current      {context.State.SessionId[..Math.Min(8, context.State.SessionId.Length)]} · {context.Options.Provider.Type}/{context.Model}",
            $"Created      {context.State.CreatedAt:yyyy-MM-dd HH:mm:ss}",
            string.Empty,
            "Recent snapshots",
        ];

        if (context.State.SavedSessions.Count == 0)
        {
            lines.Add("(no snapshots loaded)");
        }
        else
        {
            lines.AddRange(context.State.SavedSessions.Select(static session => $"{session.UpdatedAt:MM-dd HH:mm}  {session.Id}  {session.Name}"));
        }

        lines.Add(string.Empty);
        lines.Add("Use /save to persist the current session and /restore <id> to load one.");

        return new TerminalPanelViewModel
        {
            PanelId = "sessions",
            Title = "Session Tabs",
            IsActive = string.Equals(context.State.SidebarMode, TerminalSidebarModeCatalog.Sessions, StringComparison.OrdinalIgnoreCase),
            Lines = lines,
        };
    }

    private static TerminalPanelViewModel BuildErrorsPanel(TerminalSurfaceBuildContext context)
    {
        if (context.State.ErrorFeed.Count == 0)
        {
            return new TerminalPanelViewModel
            {
                PanelId = "errors",
                Title = "Errors",
                IsActive = string.Equals(context.State.SidebarMode, TerminalSidebarModeCatalog.Errors, StringComparison.OrdinalIgnoreCase),
                Lines =
                [
                    "No recorded errors for this run.",
                    "When an operation fails, details and the log file path will appear here.",
                ],
            };
        }

        List<string> lines = [];
        foreach (TerminalErrorEntry entry in context.State.ErrorFeed.OrderByDescending(static item => item.Timestamp).Take(8))
        {
            lines.Add($"{entry.Timestamp:HH:mm:ss}  {entry.Title}");
            lines.Add($"  {entry.ExceptionType}: {entry.Message}");
            if (!string.IsNullOrWhiteSpace(entry.LogFilePath))
            {
                lines.Add($"  log: {entry.LogFilePath}");
            }

            lines.Add(string.Empty);
        }

        return new TerminalPanelViewModel
        {
            PanelId = "errors",
            Title = "Errors",
            IsActive = string.Equals(context.State.SidebarMode, TerminalSidebarModeCatalog.Errors, StringComparison.OrdinalIgnoreCase),
            Lines = lines,
        };
    }

    private static int ToInt(long? value)
        => value is null ? 0 : (int)Math.Clamp(value.Value, 0L, int.MaxValue);

    private static string Summarize(string text, int maxLength)
        => string.IsNullOrWhiteSpace(text) || text.Length <= maxLength ? text : text[..Math.Max(1, maxLength - 1)] + "…";
}
