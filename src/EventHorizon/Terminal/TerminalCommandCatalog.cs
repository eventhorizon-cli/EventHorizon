using EventHorizon.Conversations;

namespace EventHorizon.Terminal;

public static class TerminalCommandCatalog
{
    private static readonly IReadOnlyList<TerminalPaletteCommandDefinition> Definitions =
    [
        new("/help", "/help", "Open the command palette overlay", "Workbench"),
        new("/stats", "/stats", "Refresh usage metrics in the inspector", "Session"),
        new("/save [name]", "/save", "Persist the current session snapshot", "Session"),
        new("/sessions", "/sessions", "Reload recent saved sessions into the sidebar", "Session"),
        new("/restore <id>", "/restore", "Restore a saved session by id", "Session"),
        new("/focus <path>", "/focus", "Highlight a workspace file or folder in Explorer", "Workspace"),
        new("/sidebar <mode>", "/sidebar", "Switch the sidebar surface between overview, files, activity, commands, sessions, or errors", "Workbench"),
        new("/clear", "/clear", "Clear the activity feed without deleting the transcript", "Session"),
        new("/reset", "/reset", "Start a fresh runtime session and clear usage totals", "Session"),
        new("/exit", "/exit", "Exit the terminal workbench", "Workbench"),
    ];

    private static readonly IReadOnlyDictionary<string, string> SidebarModeLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [TerminalSidebarModeCatalog.Overview] = "Overview",
        [TerminalSidebarModeCatalog.Files] = "Files",
        [TerminalSidebarModeCatalog.Activity] = "Activity",
        [TerminalSidebarModeCatalog.Commands] = "Commands",
        [TerminalSidebarModeCatalog.Sessions] = "Sessions",
        [TerminalSidebarModeCatalog.Errors] = "Errors",
    };

    public static IReadOnlyList<string> HelpLines =>
        Definitions.Select(static entry => $"{entry.Usage.PadRight(18)} {entry.Description}").ToList();

    public static IReadOnlyList<string> GetCommandNames() =>
        Definitions.Select(static entry => entry.Trigger).ToList();

    public static IReadOnlyList<string> GetSidebarModes() => TerminalSidebarModeCatalog.Ordered;

    public static bool TryNormalizeSidebarMode(string? value, out string mode)
    {
        string candidate = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (TerminalSidebarModeCatalog.IsKnown(candidate))
        {
            mode = candidate;
            return true;
        }

        mode = string.Empty;
        return false;
    }

    public static string GetSidebarModeLabel(string mode)
        => SidebarModeLabels.TryGetValue(mode, out string? label) ? label : mode;

    public static IReadOnlyList<TerminalPaletteItem> BuildPaletteItems(
        IReadOnlyList<TerminalCommandEntry> commandHistory,
        IReadOnlyList<ConversationSessionSummary> savedSessions,
        string? focusedPath,
        string sidebarMode)
    {
        List<TerminalPaletteItem> items = Definitions
            .Select(definition => new TerminalPaletteItem(
                definition.Trigger,
                definition.Usage,
                definition.Description,
                definition.Category,
                definition.Trigger == "/sidebar" ? $"Current sidebar: {GetSidebarModeLabel(sidebarMode)}" : string.Empty))
            .ToList();

        if (!string.IsNullOrWhiteSpace(focusedPath))
        {
            items.Add(new TerminalPaletteItem(
                $"/focus {focusedPath}",
                "/focus current",
                "Re-apply the current workspace focus",
                "Workspace",
                focusedPath));
            items.Add(new TerminalPaletteItem(
                "/focus",
                "/focus clear",
                "Clear the current workspace focus",
                "Workspace",
                "workspace root"));
        }

        items.AddRange(savedSessions
            .Take(5)
            .Select(session => new TerminalPaletteItem(
                $"/restore {session.Id}",
                session.Name,
                $"Restore saved session {session.Id}",
                "Sessions",
                $"{session.ProviderType}/{session.Model} · {session.UpdatedAt:MM-dd HH:mm}")));

        items.AddRange(commandHistory
            .OrderByDescending(static entry => entry.Timestamp)
            .Take(5)
            .Select(entry => new TerminalPaletteItem(
                entry.CommandText,
                entry.CommandText,
                "Re-run a recent command",
                "Recent",
                entry.Timestamp.ToString("HH:mm:ss"))));

        return items;
    }

    public static IReadOnlyList<TerminalPaletteItem> FilterPaletteItems(IReadOnlyList<TerminalPaletteItem> items, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return items;
        }

        string normalized = query.Trim();
        return items
            .Select(item => new { item, score = Score(item, normalized) })
            .Where(x => x.score >= 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.item.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.item.Title, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.item)
            .ToList();
    }

    public static IReadOnlyList<string> BuildCommandDeckLines(
        IReadOnlyList<TerminalCommandEntry> commandHistory,
        IReadOnlyList<ConversationSessionSummary> savedSessions,
        string? focusedPath,
        string sidebarMode)
    {
        IReadOnlyList<TerminalPaletteItem> items = BuildPaletteItems(commandHistory, savedSessions, focusedPath, sidebarMode);
        List<string> lines =
        [
            "Command palette",
            "Ctrl+K opens the overlay palette · Enter executes the selected item",
            string.Empty,
        ];

        foreach (IGrouping<string, TerminalPaletteItem> group in items.Take(12).GroupBy(static item => item.Category))
        {
            lines.Add(group.Key);
            foreach (TerminalPaletteItem item in group.Take(4))
            {
                lines.Add($"  {item.Title}");
                lines.Add($"    {item.Description}");
                if (!string.IsNullOrWhiteSpace(item.Footer))
                {
                    lines.Add($"    {item.Footer}");
                }
            }

            lines.Add(string.Empty);
        }

        lines.Add("Keyboard");
        lines.Add("  Ctrl+1..5  switch focus surfaces");
        lines.Add("  Ctrl+K     open command palette");
        lines.Add("  Ctrl+F     prefill /focus <path>");
        lines.Add("  Ctrl+R     reverse search input history");
        lines.Add("  Tab        complete commands and paths");
        lines.Add("  Ctrl+L     force a redraw");
        return lines;
    }

    public static IReadOnlyList<string> BuildPaletteLines(IReadOnlyList<TerminalCommandEntry> commandHistory, string? focusedPath)
        => BuildCommandDeckLines(commandHistory, [], focusedPath, TerminalSidebarModeCatalog.Commands);

    private static int Score(TerminalPaletteItem item, string query)
    {
        string[] haystacks = [item.CommandText, item.Title, item.Description, item.Category, item.Footer];
        var bestScore = -1;
        foreach (string haystack in haystacks)
        {
            if (string.IsNullOrWhiteSpace(haystack))
            {
                continue;
            }

            if (haystack.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                bestScore = Math.Max(bestScore, 300 - haystack.Length);
            }
            else if (haystack.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                bestScore = Math.Max(bestScore, 100 - haystack.Length);
            }
        }

        return bestScore;
    }
}

public readonly record struct TerminalPaletteCommandDefinition(string Usage, string Trigger, string Description, string Category);

public readonly record struct TerminalPaletteItem(string CommandText, string Title, string Description, string Category, string Footer = "");
