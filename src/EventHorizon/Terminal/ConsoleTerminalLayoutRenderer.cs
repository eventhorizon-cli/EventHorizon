using System.Text;

namespace EventHorizon.Terminal;

public sealed class ConsoleTerminalLayoutRenderer : ITerminalLayoutRenderer
{
    private const char InputCursorMarker = '\uE000';
    private readonly object _syncLock = new();
    private TerminalRenderFrame? _previousFrame;

    public void Render(TerminalViewModel viewModel)
    {
        lock (_syncLock)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (Console.IsOutputRedirected)
            {
                RenderFallback(viewModel);
                _previousFrame = null;
                return;
            }

            var width = Math.Max(48, SafeWindowWidth());
            var height = Math.Max(20, SafeWindowHeight());
            var nextFrame = BuildFrame(viewModel, width, height);
            var requiresFullRedraw = _previousFrame is null
                                     || _previousFrame.Width != nextFrame.Width
                                     || _previousFrame.Rows.Count != nextFrame.Rows.Count
                                     || _previousFrame.Regions.Count != nextFrame.Regions.Count;

            try
            {
                Console.CursorVisible = false;
            }
            catch
            {
                // Ignore unsupported terminals.
            }

            if (requiresFullRedraw)
            {
                FullRender(nextFrame);
            }
            else
            {
                RenderDiff(_previousFrame!, nextFrame);
            }

            RestoreCursor(nextFrame);
            _previousFrame = nextFrame;
        }
    }

    public void Reset()
    {
        lock (_syncLock)
        {
            _previousFrame = null;
            if (Console.IsOutputRedirected)
            {
                return;
            }

            try
            {
                Console.Clear();
            }
            catch
            {
                // Ignore clear failures.
            }
        }
    }

    internal static TerminalRenderFrame BuildFrame(TerminalViewModel viewModel, int width, int height)
    {
        width = Math.Max(48, width);
        height = Math.Max(20, height);
        return viewModel.ShowLaunchpad
            ? BuildLaunchpadFrame(viewModel, width, height)
            : BuildWorkbenchFrame(viewModel, width, height);
    }

    private static TerminalRenderFrame BuildWorkbenchFrame(TerminalViewModel viewModel, int width, int height)
    {
        const int headerHeight = 3;
        const int statusHeight = 2;
        const int minimumBodyHeight = 8;

        int inputViewportHeight = DetermineInputViewportHeight(viewModel.Composer, width - 4, height, headerHeight, statusHeight, minimumBodyHeight, 3, 6);
        var inputRegionHeight = inputViewportHeight + 2;
        int bodyHeight = Math.Max(minimumBodyHeight, height - headerHeight - statusHeight - inputRegionHeight);

        FrameBuilder builder = new(width);
        builder.AddUniformLine("header:title", BuildTitleLine(viewModel, width), viewModel.IsStreaming ? ConsoleColor.Yellow : ConsoleColor.DarkBlue);
        builder.AddUniformLine("header:context", Summarize(viewModel.HeaderContext, width).PadRight(width), ConsoleColor.DarkGray);
        builder.AddUniformLine("header:summary", BuildWorkbenchSummaryLine(viewModel, width), ConsoleColor.DarkGray);

        AppendPanelStrip(
            builder,
            [new SurfacePanel("surface:main", viewModel.MainSurface.Panel)],
            [width],
            bodyHeight);

        builder.AddUniformLine("footer:status:primary", viewModel.StatusBar.PrimaryText.PadRight(width), viewModel.IsStreaming ? ConsoleColor.Yellow : ConsoleColor.DarkBlue);
        builder.AddUniformLine("footer:status:secondary", Summarize(viewModel.StatusBar.SecondaryText, width).PadRight(width), ConsoleColor.DarkGray);
            (int cursorLeft, int cursorTop) = builder.AddInputBox(viewModel.Composer, inputViewportHeight);
            if (viewModel.Overlay.IsOpen)
            {
                AppendOverlay(builder, viewModel.Overlay, width, height - inputRegionHeight - statusHeight);
            }

        return builder.Build(cursorLeft, cursorTop, cursorVisible: false);
    }

    private static TerminalRenderFrame BuildLaunchpadFrame(TerminalViewModel viewModel, int width, int height)
    {
        const int headerHeight = 6;
        const int statusHeight = 2;
        const int minimumPanelHeight = 8;

        int inputViewportHeight = DetermineInputViewportHeight(viewModel.Composer, width - 4, height, headerHeight, statusHeight, minimumPanelHeight, 3, 5);
        var inputRegionHeight = inputViewportHeight + 2;
        int panelHeight = Math.Max(minimumPanelHeight, height - headerHeight - statusHeight - inputRegionHeight);

        FrameBuilder builder = new(width);
        builder.AddUniformLine("header:title", BuildTitleLine(viewModel, width), ConsoleColor.DarkBlue);
        builder.AddUniformLine("header:context", Summarize(viewModel.HeaderContext, width).PadRight(width), ConsoleColor.DarkGray);
        builder.AddUniformLine("header:badges", BuildBadgeLine(viewModel, width), ConsoleColor.DarkBlue);
        builder.AddUniformLine("header:tabs", BuildTabsLine(viewModel, width), ConsoleColor.White);
        builder.AddUniformLine("header:breadcrumbs", BuildBreadcrumbLine(viewModel, width), ConsoleColor.DarkGray);
        builder.AddUniformLine("header:navigation", new string('─', width), ConsoleColor.DarkGray);

        AppendPanelStrip(builder, [new SurfacePanel("surface:launchpad", viewModel.LaunchpadSurface.Panel)], [width], panelHeight);

        builder.AddUniformLine("footer:status:primary", viewModel.StatusBar.PrimaryText.PadRight(width), ConsoleColor.Yellow);
        builder.AddUniformLine("footer:status:secondary", Summarize(viewModel.StatusBar.SecondaryText, width).PadRight(width), ConsoleColor.DarkGray);
        (int cursorLeft, int cursorTop) = builder.AddInputBox(viewModel.Composer, inputViewportHeight);
        if (viewModel.Overlay.IsOpen)
        {
            AppendOverlay(builder, viewModel.Overlay, width, height - inputRegionHeight - statusHeight);
        }

        return builder.Build(cursorLeft, cursorTop, cursorVisible: false);
    }

    private static int DetermineInputViewportHeight(
        TerminalComposerViewModel composer,
        int availableWidth,
        int totalHeight,
        int headerHeight,
        int statusHeight,
        int minimumBodyHeight,
        int minimumViewportHeight,
        int maximumViewportHeight)
    {
        IReadOnlyList<InputVisualLine> inputLines = BuildInputVisualLines(composer.Buffer, Math.Max(1, availableWidth));
        int desiredViewportHeight = Math.Clamp(Math.Max(minimumViewportHeight, inputLines.Count), minimumViewportHeight, maximumViewportHeight);
        int maxViewportHeight = Math.Max(1, totalHeight - headerHeight - statusHeight - minimumBodyHeight - 2);
        return Math.Min(desiredViewportHeight, maxViewportHeight);
    }

    internal static IReadOnlyList<string> DiffRegions(TerminalRenderFrame? previousFrame, TerminalRenderFrame nextFrame)
    {
        if (previousFrame is null
            || previousFrame.Width != nextFrame.Width
            || previousFrame.Regions.Count != nextFrame.Regions.Count)
        {
            return nextFrame.Regions.Select(static region => region.Key).ToArray();
        }

        Dictionary<string, RenderRegion> previous = previousFrame.Regions.ToDictionary(static region => region.Key, StringComparer.Ordinal);
        List<string> dirty = [];
        foreach (RenderRegion region in nextFrame.Regions)
        {
            if (!previous.TryGetValue(region.Key, out RenderRegion? oldRegion) || !oldRegion.ContentEquals(region))
            {
                dirty.Add(region.Key);
            }
        }

        return dirty;
    }

    private static void AppendPanelStrip(FrameBuilder builder, IReadOnlyList<SurfacePanel> panels, IReadOnlyList<int> widths, int height)
    {
        var stripTop = builder.RowCount;
        List<IReadOnlyList<StyledLine>> wrappedLines = [];
        for (int index = 0; index < panels.Count; index++)
        {
            wrappedLines.Add(BuildPanelLines(panels[index].Panel, widths[index] - 2, height - 4));
        }

        builder.AddSegments(BuildStripRow(panels, widths, static (panel, width) => BuildPanelBorderRow(width, panel.Panel.IsActive, '╭', '─', '╮')));
        builder.AddSegments(BuildStripRow(panels, widths, static (panel, width) => BuildPanelHeaderRow(width, panel.Panel)));
        builder.AddSegments(BuildStripRow(panels, widths, static (panel, width) => BuildPanelBorderRow(width, panel.Panel.IsActive, '├', '─', '┤')));

        for (int row = 0; row < Math.Max(0, height - 4); row++)
        {
            var rowIndex = row;
            builder.AddSegments(BuildStripRow(panels, widths, (panel, width, panelIndex) =>
            {
                StyledLine line = rowIndex < wrappedLines[panelIndex].Count ? wrappedLines[panelIndex][rowIndex] : StyledLine.Empty;
                return BuildPanelContentRow(width, panel.Panel, line);
            }));
        }

        builder.AddSegments(BuildStripRow(panels, widths, static (panel, width) => BuildPanelBorderRow(width, panel.Panel.IsActive, '╰', '─', '╯')));

        for (int index = 0; index < panels.Count; index++)
        {
            int left = CalculatePanelLeft(widths, index);
            builder.AddRegion(new RenderRegion($"{panels[index].RegionBase}:border", left,
            [
                new RenderRegionRow(stripTop, BuildPanelBorderRow(widths[index], panels[index].Panel.IsActive, '╭', '─', '╮')),
                new RenderRegionRow(stripTop + 2, BuildPanelBorderRow(widths[index], panels[index].Panel.IsActive, '├', '─', '┤')),
                new RenderRegionRow(stripTop + height - 1, BuildPanelBorderRow(widths[index], panels[index].Panel.IsActive, '╰', '─', '╯')),
            ]));
            builder.AddRegion(new RenderRegion($"{panels[index].RegionBase}:header", left,
            [new RenderRegionRow(stripTop + 1, BuildPanelHeaderRow(widths[index], panels[index].Panel))]));

            List<RenderRegionRow> bodyRows = [];
            for (int row = 0; row < Math.Max(0, height - 4); row++)
            {
                StyledLine line = row < wrappedLines[index].Count ? wrappedLines[index][row] : StyledLine.Empty;
                bodyRows.Add(new RenderRegionRow(stripTop + 3 + row, BuildPanelContentRow(widths[index], panels[index].Panel, line)));
            }

            builder.AddRegion(new RenderRegion($"{panels[index].RegionBase}:viewport", left, bodyRows));
        }
    }

    private static IReadOnlyList<StyledSegment> BuildStripRow(
        IReadOnlyList<SurfacePanel> panels,
        IReadOnlyList<int> widths,
        Func<SurfacePanel, int, RenderRow> buildRow)
        => BuildStripRow(panels, widths, (panel, width, _) => buildRow(panel, width));

    private static IReadOnlyList<StyledSegment> BuildStripRow(
        IReadOnlyList<SurfacePanel> panels,
        IReadOnlyList<int> widths,
        Func<SurfacePanel, int, int, RenderRow> buildRow)
    {
        List<StyledSegment> segments = [];
        for (int index = 0; index < panels.Count; index++)
        {
            if (index > 0)
            {
                segments.Add(new StyledSegment(" ", ConsoleColor.Black));
            }

            segments.AddRange(buildRow(panels[index], widths[index], index).Segments);
        }

        return segments;
    }

    private static void AppendOverlay(FrameBuilder builder, TerminalOverlayViewModel overlay, int width, int bodyBottom)
    {
        int modalWidth = Math.Clamp(width * 2 / 3, 44, Math.Max(44, width - 8));
        int visibleBodyHeight = Math.Clamp(Math.Max(6, overlay.Lines.Count), 6, 14);
        var totalHeight = visibleBodyHeight + 4;
        int left = Math.Max(0, (width - modalWidth) / 2);
        int top = Math.Max(1, Math.Min(Math.Max(1, bodyBottom / 2 - totalHeight / 2), Math.Max(1, builder.RowCount - totalHeight - 1)));

        RenderRow topBorder = RenderRow.Create(modalWidth, [new StyledSegment(BuildInputBorderLine(modalWidth, overlay.Title, '╔', '═', '╗'), ConsoleColor.Magenta)]);
        RenderRow subtitle = RenderRow.Create(modalWidth,
        [
            new StyledSegment("║", ConsoleColor.Magenta),
            new StyledSegment(Summarize($" {overlay.Subtitle}", modalWidth - 2).PadRight(modalWidth - 2), ConsoleColor.DarkGray),
            new StyledSegment("║", ConsoleColor.Magenta),
        ]);
        RenderRow divider = RenderRow.Create(modalWidth, [new StyledSegment(BuildInputBorderLine(modalWidth, $" query: {overlay.Query}", '╟', '─', '╢'), ConsoleColor.Magenta)]);
        List<RenderRegionRow> rows =
        [
            new(top, topBorder),
            new(top + 1, subtitle),
            new(top + 2, divider),
        ];

        IReadOnlyList<StyledLine> wrappedLines = overlay.Lines.SelectMany(line => Wrap(line, Math.Max(1, modalWidth - 4)).Select(static item => new StyledLine(item, ConsoleColor.DarkGray))).ToList();
        for (int index = 0; index < visibleBodyHeight; index++)
        {
            StyledLine line = index < wrappedLines.Count ? wrappedLines[index] : StyledLine.Empty;
            ConsoleColor color = DetermineOverlayLineColor(line.Text);
            RenderRow row = RenderRow.Create(modalWidth,
            [
                new StyledSegment("║ ", ConsoleColor.Magenta),
                new StyledSegment(line.Text.PadRight(modalWidth - 4), color),
                new StyledSegment(" ║", ConsoleColor.Magenta),
            ]);
            rows.Add(new RenderRegionRow(top + 3 + index, row));
        }

        RenderRow bottom = RenderRow.Create(modalWidth, [new StyledSegment(BuildInputBorderLine(modalWidth, " Enter run · Esc close · ↑/↓ move ", '╚', '═', '╝'), ConsoleColor.Magenta)]);
        rows.Add(new RenderRegionRow(top + visibleBodyHeight + 3, bottom));
        builder.AddRegion(new RenderRegion("overlay:command-palette", left, rows));
    }

    private static int CalculatePanelLeft(IReadOnlyList<int> widths, int panelIndex)
    {
        var left = 0;
        for (int index = 0; index < panelIndex; index++)
        {
            left += widths[index] + 1;
        }

        return left;
    }

    private static RenderRow BuildPanelBorderRow(int width, bool isActive, char left, char fill, char right)
        => RenderRow.Create(width,
        [
            new StyledSegment(left.ToString(), ConsoleColor.DarkBlue),
            new StyledSegment(new string(fill, Math.Max(0, width - 2)), ConsoleColor.DarkBlue),
            new StyledSegment(right.ToString(), ConsoleColor.DarkBlue),
        ]);

    private static RenderRow BuildPanelHeaderRow(int width, TerminalPanelViewModel panel)
    {
        var title = panel.IsActive ? $"▣ {panel.Title}" : $"  {panel.Title}";
        return RenderRow.Create(width,
        [
            new StyledSegment("│", ConsoleColor.DarkBlue),
            new StyledSegment(Summarize(title, Math.Max(0, width - 2)).PadRight(Math.Max(0, width - 2)), ConsoleColor.DarkBlue),
            new StyledSegment("│", ConsoleColor.DarkBlue),
        ]);
    }

    private static RenderRow BuildPanelContentRow(int width, TerminalPanelViewModel panel, StyledLine line)
        => RenderRow.Create(width,
        [
            new StyledSegment("│", ConsoleColor.DarkBlue),
            new StyledSegment(line.Text.PadRight(Math.Max(0, width - 2)), line.Color),
            new StyledSegment("│", ConsoleColor.DarkBlue),
        ]);

    private static string BuildTitleLine(TerminalViewModel viewModel, int width)
        => Summarize($"{viewModel.StatusIndicator} {viewModel.Title} · {viewModel.Subtitle}", width).PadRight(width);

    private static string BuildBadgeLine(TerminalViewModel viewModel, int width)
    {
        string text = viewModel.HeaderBadges.Count == 0
            ? string.Empty
            : string.Join("   ", viewModel.HeaderBadges);
        return Summarize(text, width).PadRight(width);
    }

    private static string BuildTabsLine(TerminalViewModel viewModel, int width)
    {
        if (viewModel.SessionTabs.Count == 0)
        {
            return new string(' ', width);
        }

        string text = string.Join("  ", viewModel.SessionTabs.Select(static tab =>
            tab.IsActive
                ? $"[{tab.Title} · {tab.Subtitle}]"
                : $" {tab.Title} · {tab.Subtitle} "));
        return Summarize(text, width).PadRight(width);
    }

    private static string BuildBreadcrumbLine(TerminalViewModel viewModel, int width)
    {
        if (viewModel.Breadcrumbs.Count == 0)
        {
            return new string(' ', width);
        }

        return Summarize(string.Join(" › ", viewModel.Breadcrumbs), width).PadRight(width);
    }

    private static string BuildNavigationLine(TerminalViewModel viewModel, int width)
    {
        if (viewModel.Navigation.Count == 0)
        {
            return new string('─', width);
        }

        string text = string.Join("  ", viewModel.Navigation.Select(static item =>
        {
            string badge = string.IsNullOrWhiteSpace(item.Badge) ? string.Empty : $" {item.Badge}";
            var active = item.IsActive ? "●" : "○";
            return $"{active} {item.Label} {item.Shortcut}{badge}";
        }));
        return Summarize(text, width).PadRight(width);
    }

    private static string BuildWorkbenchSummaryLine(TerminalViewModel viewModel, int width)
    {
        string location = viewModel.Breadcrumbs.Count == 0
            ? "coding session"
            : string.Join(" › ", viewModel.Breadcrumbs);
        var state = viewModel.IsStreaming ? "streaming" : "ready";
        return Summarize($"{state} · {location}", width).PadRight(width);
    }

        private static IReadOnlyList<StyledLine> BuildPanelLines(TerminalPanelViewModel panel, int width, int height)
        {
            var isInsideCodeFence = false;
            List<StyledLine> wrappedLines = new();

            // Maintain current message color so that message body lines (which are prefixed with two spaces)
            // inherit the header color (e.g. streaming -> dark gray, assistant final -> white).
            ConsoleColor currentMessageColor = ConsoleColor.White;

            // Iterate with index so we can merge a header line with the following body line
            // to keep role and output on the same line.
            for (int i = 0; i < panel.Lines.Count; i++)
            {
                string rawLine = panel.Lines[i];

                // If this is a header line and the next line is a body line (starts with two spaces),
                // merge them into a single line: "HEADER  body..." so role and content appear on one line.
                if (!rawLine.StartsWith("  ", StringComparison.Ordinal) && i + 1 < panel.Lines.Count && panel.Lines[i + 1].StartsWith("  ", StringComparison.Ordinal))
                {
                    string next = panel.Lines[i + 1];
                    rawLine = rawLine + " " + next.TrimStart();
                    // skip the next line since we've merged it
                    i++;
                }

                // Determine a candidate color using existing logic which also handles code-fence toggling.
                ConsoleColor candidate = DetermineLineColor(panel, rawLine, ref isInsideCodeFence);

                // If this raw line is a header (not a body line which starts with two spaces), update currentMessageColor.
                bool isBodyLine = rawLine.StartsWith("  ", StringComparison.Ordinal);
                if (!isBodyLine)
                {
                    currentMessageColor = candidate;
                }

                // For body lines we normally want to inherit current message color, but preserve strong syntax colors
                // such as code fences, diffs and explicit activity colors returned by DetermineLineColor.
                ConsoleColor finalColor;
                if (isBodyLine)
                {
                    // Preserve special colors; otherwise inherit the message header color.
                    switch (candidate)
                    {
                        case ConsoleColor.DarkYellow: // code fence markers
                        case ConsoleColor.Yellow: // patch headers
                        case ConsoleColor.DarkBlue: // additions / emphasis
                        case ConsoleColor.Red: // deletions / errors
                        case ConsoleColor.Magenta:
                            finalColor = candidate;
                            break;
                        default:
                            finalColor = currentMessageColor;
                            break;
                    }
                }
                else
                {
                    finalColor = candidate;
                }

                foreach (string wrapped in Wrap(rawLine, width))
                {
                    wrappedLines.Add(new StyledLine(wrapped, finalColor));
                }
            }

            int visibleCount = Math.Max(0, height);
            if (visibleCount == 0)
            {
                return [];
            }

            int scrollOffset = Math.Max(0, panel.ScrollOffset);
            int start = Math.Max(0, wrappedLines.Count - visibleCount - scrollOffset);
            return wrappedLines.Skip(start).Take(visibleCount).ToList();
        }

    private static IReadOnlyList<string> Wrap(string text, int width)
    {
        if (width <= 0)
        {
            return [string.Empty];
        }

        if (string.IsNullOrEmpty(text))
        {
            return [string.Empty];
        }

        List<string> lines = [];
        foreach (string rawLine in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var remaining = rawLine;
            if (remaining.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            while (remaining.Length > width)
            {
                int split = remaining.LastIndexOf(' ', Math.Min(width, remaining.Length - 1), Math.Min(width, remaining.Length));
                if (split <= 0)
                {
                    split = width;
                }

                lines.Add(remaining[..split].TrimEnd());
                remaining = remaining[split..].TrimStart();
            }

            lines.Add(remaining);
        }

        return lines;
    }

    private static ConsoleColor DetermineLineColor(TerminalPanelViewModel panel, string line, ref bool isInsideCodeFence)
    {
        string trimmed = line.TrimStart();
        if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
        {
            isInsideCodeFence = !isInsideCodeFence;
            return ConsoleColor.DarkYellow;
        }

        if (IsPatchHeader(trimmed))
        {
            return ConsoleColor.Yellow;
        }

            if (trimmed.StartsWith('+') && !trimmed.StartsWith("+++", StringComparison.Ordinal))
            {
                return ConsoleColor.DarkBlue;
            }

        if (trimmed.StartsWith('-') && !trimmed.StartsWith("---", StringComparison.Ordinal))
        {
            return ConsoleColor.Red;
        }

            if (isInsideCodeFence)
            {
                return ConsoleColor.DarkYellow;
            }

        if (string.Equals(panel.PanelId, TerminalPanelCatalog.Activity, StringComparison.OrdinalIgnoreCase) || panel.PanelId.Contains("activity", StringComparison.OrdinalIgnoreCase))
        {
            return DetermineActivityColor(trimmed);
        }

        if (string.Equals(panel.PanelId, TerminalPanelCatalog.Commands, StringComparison.OrdinalIgnoreCase) || panel.PanelId.Contains("command", StringComparison.OrdinalIgnoreCase))
        {
            if (trimmed.StartsWith("/", StringComparison.Ordinal))
            {
                return ConsoleColor.Yellow;
            }

            if (trimmed is "Keyboard" or "Recent commands" or "Slash commands")
            {
                return ConsoleColor.DarkBlue;
            }
        }

        if (string.Equals(panel.PanelId, TerminalPanelCatalog.Explorer, StringComparison.OrdinalIgnoreCase) && trimmed.StartsWith("●", StringComparison.Ordinal))
        {
            return ConsoleColor.DarkBlue;
        }

        if (string.Equals(panel.PanelId, TerminalPanelCatalog.Conversation, StringComparison.OrdinalIgnoreCase))
        {
            // Color special conversation role header lines (TOOL, TOOL RESULT, THOUGHT)
            if (trimmed.Contains("TOOL CALL", StringComparison.Ordinal))
            {
                return ConsoleColor.Yellow;
            }

            if (trimmed.Contains("TOOL RESULT", StringComparison.Ordinal))
            {
                return ConsoleColor.Magenta;
            }

            if (trimmed.Contains("THOUGHT", StringComparison.Ordinal))
            {
                return ConsoleColor.DarkYellow;
            }

            if (trimmed.Contains("USER", StringComparison.Ordinal))
            {
                return ConsoleColor.DarkBlue;
            }

            if (trimmed.Contains("ASSISTANT", StringComparison.Ordinal))
            {
                // Final assistant replies should be bright (white). Streaming (processing) assistant lines
                // are marked with "streaming" in the header and rendered as dark gray.
                return ConsoleColor.White;
            }

            if (trimmed.Contains("streaming", StringComparison.OrdinalIgnoreCase))
            {
                return ConsoleColor.DarkGray;
            }
        }

        if (string.Equals(panel.PanelId, TerminalPanelCatalog.Inspector, StringComparison.OrdinalIgnoreCase) && trimmed is "Usage" or "Recent sessions" or "Last prompt")
        {
            return ConsoleColor.DarkBlue;
        }

        if (panel.PanelId is "sessions")
        {
            if (trimmed is "Recent snapshots")
            {
                return ConsoleColor.DarkBlue;
            }

            if (trimmed.Contains("Use /save", StringComparison.Ordinal))
            {
                return ConsoleColor.Yellow;
            }
        }

        if (panel.PanelId is "overview" or "overview-dock")
        {
                if (trimmed.StartsWith("Focus", StringComparison.Ordinal)
                 || trimmed.StartsWith("Workspace", StringComparison.Ordinal)
                 || trimmed.StartsWith("Recent", StringComparison.Ordinal)
                 || trimmed.StartsWith("Usage", StringComparison.Ordinal)
                 || trimmed.StartsWith("Model", StringComparison.Ordinal)
                 || trimmed.StartsWith("Snapshots", StringComparison.Ordinal)
                 || trimmed.StartsWith("Prompt", StringComparison.Ordinal)
                 || trimmed.StartsWith("Actions", StringComparison.Ordinal))
                {
                return ConsoleColor.DarkBlue;
                }
        }

        if (string.Equals(panel.PanelId, "launchpad", StringComparison.OrdinalIgnoreCase))
        {
            if (trimmed.Contains("[ready]", StringComparison.OrdinalIgnoreCase))
            {
                return ConsoleColor.DarkBlue;
            }

            if (trimmed.Contains("[needs setup]", StringComparison.OrdinalIgnoreCase))
            {
                return ConsoleColor.Yellow;
            }

            if (trimmed is "Next" or "Setup")
            {
                return ConsoleColor.DarkBlue;
            }
        }

        return ConsoleColor.DarkGray;
    }

    private static ConsoleColor DetermineOverlayLineColor(string line)
    {
        string trimmed = line.TrimStart();
        if (trimmed.StartsWith("›", StringComparison.Ordinal))
        {
            return ConsoleColor.DarkBlue;
        }

        if (trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            return ConsoleColor.Yellow;
        }

        if (trimmed.Contains("Session", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("Workbench", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("Workspace", StringComparison.OrdinalIgnoreCase))
        {
            return ConsoleColor.DarkGray;
        }

        return ConsoleColor.White;
    }

    private static ConsoleColor DetermineActivityColor(string trimmed)
    {
        if (trimmed.Contains("[error]", StringComparison.OrdinalIgnoreCase))
        {
            return ConsoleColor.Red;
        }

        if (trimmed.Contains("[tool]", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("[tool-result]", StringComparison.OrdinalIgnoreCase))
        {
            return ConsoleColor.Yellow;
        }

        if (trimmed.Contains("[save]", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("[restore]", StringComparison.OrdinalIgnoreCase))
        {
            return ConsoleColor.DarkBlue;
        }

        if (trimmed.Contains("[prompt]", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("[response]", StringComparison.OrdinalIgnoreCase))
        {
            return ConsoleColor.DarkBlue;
        }

        return ConsoleColor.DarkGray;
    }

    private static bool IsPatchHeader(string trimmed)
        => trimmed.StartsWith("*** Begin Patch", StringComparison.Ordinal)
            || trimmed.StartsWith("*** Update File:", StringComparison.Ordinal)
            || trimmed.StartsWith("*** Add File:", StringComparison.Ordinal)
            || trimmed.StartsWith("*** End Patch", StringComparison.Ordinal)
            || trimmed.StartsWith("@@", StringComparison.Ordinal)
            || trimmed.StartsWith("diff --git", StringComparison.Ordinal)
            || trimmed.StartsWith("index ", StringComparison.Ordinal)
            || trimmed.StartsWith("--- ", StringComparison.Ordinal)
            || trimmed.StartsWith("+++ ", StringComparison.Ordinal);

    private static void RenderFallback(TerminalViewModel viewModel)
    {
        Console.WriteLine($"{viewModel.StatusIndicator} {viewModel.Title}");
        Console.WriteLine(viewModel.HeaderContext);
        Console.WriteLine(string.Join(" / ", viewModel.Breadcrumbs));
        Console.WriteLine(viewModel.StatusBar.PrimaryText);
        Console.WriteLine(viewModel.StatusBar.SecondaryText);
        Console.WriteLine();

        TerminalPanelViewModel panel = viewModel.ShowLaunchpad
            ? viewModel.LaunchpadSurface.Panel
            : viewModel.MainSurface.Panel;
        Console.WriteLine($"[{panel.Title}]");
        foreach (string line in panel.Lines.TakeLast(20))
        {
            Console.WriteLine(line);
        }
    }

    private static void FullRender(TerminalRenderFrame frame)
    {
        try
        {
            Console.Clear();
        }
        catch
        {
            // Ignore clear failures.
        }

        for (int rowIndex = 0; rowIndex < frame.Rows.Count; rowIndex++)
        {
            WriteRow(frame.Rows[rowIndex], rowIndex);
        }

        foreach (RenderRegion region in frame.Regions.Where(static region => region.Left > 0 || region.Key.StartsWith("overlay:", StringComparison.Ordinal)))
        {
            WriteRegion(region);
        }
    }

    private static void RenderDiff(TerminalRenderFrame previousFrame, TerminalRenderFrame nextFrame)
    {
        HashSet<string> dirty = DiffRegions(previousFrame, nextFrame).ToHashSet(StringComparer.Ordinal);
        foreach (RenderRegion region in nextFrame.Regions)
        {
            if (dirty.Contains(region.Key))
            {
                WriteRegion(region);
            }
        }
    }

    private static void WriteRegion(RenderRegion region)
    {
        foreach (RenderRegionRow regionRow in region.Rows)
        {
            try
            {
                Console.SetCursorPosition(region.Left, regionRow.Top);
            }
            catch
            {
                return;
            }

            foreach (StyledSegment segment in regionRow.Row.Segments)
            {
                if (segment.Text.Length == 0)
                {
                    continue;
                }

                Console.ForegroundColor = segment.Color;
                Console.Write(segment.Text);
            }

            Console.ResetColor();
        }
    }

    private static void WriteRow(RenderRow row, int rowIndex)
    {
        try
        {
            Console.SetCursorPosition(0, rowIndex);
        }
        catch
        {
            return;
        }

        foreach (StyledSegment segment in row.Segments)
        {
            if (segment.Text.Length == 0)
            {
                continue;
            }

            Console.ForegroundColor = segment.Color;
            Console.Write(segment.Text);
        }

        Console.ResetColor();
    }

    private static void RestoreCursor(TerminalRenderFrame frame)
    {
        try
        {
            Console.CursorVisible = frame.CursorVisible;
            Console.SetCursorPosition(frame.CursorLeft, frame.CursorTop);
        }
        catch
        {
            // Ignore cursor failures.
        }
    }

    private static int SafeWindowWidth()
    {
        try
        {
            return Console.WindowWidth;
        }
        catch
        {
            return 120;
        }
    }

    private static int SafeWindowHeight()
    {
        try
        {
            return Console.WindowHeight;
        }
        catch
        {
            return 36;
        }
    }

    private static IReadOnlyList<InputVisualLine> BuildInputVisualLines(string buffer, int availableWidth)
    {
        if (availableWidth <= 0 || string.IsNullOrEmpty(buffer))
        {
            return [new InputVisualLine(0, string.Empty)];
        }

        string normalized = buffer.Replace("\r\n", "\n", StringComparison.Ordinal);
        List<InputVisualLine> lines = [];
        StringBuilder currentLine = new();
        var currentLineStart = 0;

        for (int index = 0; index < normalized.Length; index++)
        {
            var current = normalized[index];
            if (current == '\n')
            {
                lines.Add(new InputVisualLine(currentLineStart, currentLine.ToString()));
                currentLine.Clear();
                currentLineStart = index + 1;
                continue;
            }

            if (currentLine.Length == availableWidth)
            {
                lines.Add(new InputVisualLine(currentLineStart, currentLine.ToString()));
                currentLine.Clear();
                currentLineStart = index;
            }

            currentLine.Append(current);
        }

        lines.Add(new InputVisualLine(currentLineStart, currentLine.ToString()));
        return lines;
    }

    private static IReadOnlyList<InputVisualLine> BuildInputVisualLines(string buffer, int cursorIndex, int availableWidth)
    {
        string normalized = buffer.Replace("\r\n", "\n", StringComparison.Ordinal);
        int clampedCursorIndex = Math.Clamp(cursorIndex, 0, normalized.Length);
        string withCursor = normalized.Insert(clampedCursorIndex, InputCursorMarker.ToString());
        return BuildInputVisualLines(withCursor, availableWidth);
    }

    private static InputViewport BuildInputViewport(string buffer, int cursorIndex, int availableWidth, int viewportHeight)
    {
        IReadOnlyList<InputVisualLine> allLines = BuildInputVisualLines(buffer, cursorIndex, availableWidth);
        var cursorLineIndex = 0;
        var cursorColumn = 0;

        for (int index = 0; index < allLines.Count; index++)
        {
            int markerIndex = allLines[index].Text.IndexOf(InputCursorMarker);
            if (markerIndex >= 0)
            {
                cursorLineIndex = index;
                cursorColumn = markerIndex;
                break;
            }
        }

        int visibleHeight = Math.Max(1, viewportHeight);
        int viewportStart = Math.Clamp(cursorLineIndex - visibleHeight + 1, 0, Math.Max(0, allLines.Count - visibleHeight));
        List<InputVisualLine> visibleLines = allLines.Skip(viewportStart).Take(visibleHeight).ToList();
        var cursorRow = cursorLineIndex - viewportStart;
        return new InputViewport(visibleLines, cursorRow, cursorColumn, viewportStart, allLines.Count);
    }

    private static string BuildInputBorderLine(int width, string title, char left, char fill, char right)
    {
        int innerWidth = Math.Max(0, width - 2);
        string label = string.IsNullOrWhiteSpace(title) ? string.Empty : $" {title.Trim()} ";
        if (label.Length > innerWidth)
        {
            label = label[..innerWidth];
        }

        return left + label + new string(fill, Math.Max(0, innerWidth - label.Length)) + right;
    }

    internal sealed class TerminalRenderFrame
    {
        public TerminalRenderFrame(int width, IReadOnlyList<RenderRow> rows, IReadOnlyList<RenderRegion> regions, int cursorLeft, int cursorTop, bool cursorVisible)
        {
            Width = width;
            Rows = rows;
            Regions = regions;
            CursorLeft = cursorLeft;
            CursorTop = cursorTop;
            CursorVisible = cursorVisible;
        }

        public int Width { get; }
        public IReadOnlyList<RenderRow> Rows { get; }
        public IReadOnlyList<RenderRegion> Regions { get; }
        public int CursorLeft { get; }
        public int CursorTop { get; }
        public bool CursorVisible { get; }
    }

    internal sealed class RenderRegion
    {
        public RenderRegion(string key, int left, IReadOnlyList<RenderRegionRow> rows)
        {
            Key = key;
            Left = left;
            Rows = rows;
        }

        public string Key { get; }
        public int Left { get; }
        public IReadOnlyList<RenderRegionRow> Rows { get; }

        public bool ContentEquals(RenderRegion? other)
        {
            if (other is null || Left != other.Left || Rows.Count != other.Rows.Count)
            {
                return false;
            }

            for (int index = 0; index < Rows.Count; index++)
            {
                if (Rows[index].Top != other.Rows[index].Top || !Rows[index].Row.ContentEquals(other.Rows[index].Row))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal readonly record struct RenderRegionRow(int Top, RenderRow Row);

    internal sealed class RenderRow
    {
        public RenderRow(IReadOnlyList<StyledSegment> segments)
        {
            Segments = segments;
        }

        public IReadOnlyList<StyledSegment> Segments { get; }

        public bool ContentEquals(RenderRow? other)
        {
            if (other is null || Segments.Count != other.Segments.Count)
            {
                return false;
            }

            for (int index = 0; index < Segments.Count; index++)
            {
                if (!Segments[index].Equals(other.Segments[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public static RenderRow Create(int width, IEnumerable<StyledSegment> segments)
        {
            List<StyledSegment> normalized = [];
            var written = 0;
            foreach (StyledSegment segment in segments)
            {
                if (written >= width || segment.Text.Length == 0)
                {
                    continue;
                }

                var text = segment.Text;
                if (text.Length > width - written)
                {
                    text = text[..(width - written)];
                }

                AppendSegment(normalized, new StyledSegment(text, segment.Color));
                written += text.Length;
            }

            if (written < width)
            {
                AppendSegment(normalized, new StyledSegment(new string(' ', width - written), ConsoleColor.Black));
            }

            return new RenderRow(normalized);
        }

        private static void AppendSegment(List<StyledSegment> segments, StyledSegment next)
        {
            if (next.Text.Length == 0)
            {
                return;
            }

            if (segments.Count > 0 && segments[^1].Color == next.Color)
            {
                StyledSegment previous = segments[^1];
                segments[^1] = previous with { Text = previous.Text + next.Text };
                return;
            }

            segments.Add(next);
        }
    }

    internal readonly record struct StyledSegment(string Text, ConsoleColor Color);

    private sealed class FrameBuilder
    {
        private readonly int _width;
        private readonly List<RenderRow> _rows = [];
        private readonly List<RenderRegion> _regions = [];

        public FrameBuilder(int width)
        {
            _width = width;
        }

        public int RowCount => _rows.Count;

        public void AddUniformLine(string key, string text, ConsoleColor color)
        {
            var rowIndex = _rows.Count;
            RenderRow row = RenderRow.Create(_width, [new StyledSegment(text, color)]);
            _rows.Add(row);
            _regions.Add(new RenderRegion(key, 0, [new RenderRegionRow(rowIndex, row)]));
        }

        public void AddSegments(IEnumerable<StyledSegment> segments)
            => _rows.Add(RenderRow.Create(_width, segments));

        public void AddRegion(RenderRegion region)
            => _regions.Add(region);

        public (int CursorLeft, int CursorTop) AddInputBox(TerminalComposerViewModel composer, int viewportHeight)
        {
            int contentWidth = Math.Max(1, _width - 4);
            InputViewport viewport = BuildInputViewport(composer.Buffer, composer.CursorIndex, contentWidth, viewportHeight);
            string title = string.IsNullOrWhiteSpace(composer.Hint)
                ? composer.Title
                : $"{composer.Title} · {Summarize(composer.Hint, Math.Max(12, _width / 2))}";
            var top = _rows.Count;
            List<RenderRegionRow> rows = [];

            RenderRow topBorder = RenderRow.Create(_width, [new StyledSegment(BuildInputBorderLine(_width, title, '╭', '─', '╮'), ConsoleColor.DarkBlue)]);
            _rows.Add(topBorder);
            rows.Add(new RenderRegionRow(top, topBorder));

            int visibleHeight = Math.Max(1, viewportHeight);
            for (int index = 0; index < visibleHeight; index++)
            {
                var prefix = index == 0 ? composer.PromptLabel : "·";
                string text = index < viewport.VisibleLines.Count
                    ? viewport.VisibleLines[index].Text.Replace(InputCursorMarker, '|').PadRight(contentWidth)
                    : new string(' ', contentWidth);
                RenderRow row = RenderRow.Create(_width,
                [
                    // Use deep-blue for the input border/prompt marker and white for the input text.
                    new StyledSegment($"│{prefix}", ConsoleColor.DarkBlue),
                    new StyledSegment(text, ConsoleColor.White),
                    new StyledSegment(" │", ConsoleColor.DarkBlue),
                ]);
                _rows.Add(row);
                rows.Add(new RenderRegionRow(top + 1 + index, row));
            }

            string footer = viewport.TotalLineCount > viewport.VisibleLines.Count
                ? $"scroll {viewport.FirstVisibleLineIndex + 1}-{viewport.FirstVisibleLineIndex + viewport.VisibleLines.Count}/{viewport.TotalLineCount}"
                : Summarize(string.IsNullOrWhiteSpace(composer.Metadata) ? $"{composer.PromptLabel} Enter submit · multiline prompt supported" : composer.Metadata, _width - 4);
            RenderRow bottomBorder = RenderRow.Create(_width, [new StyledSegment(BuildInputBorderLine(_width, footer, '╰', '─', '╯'), ConsoleColor.DarkBlue)]);
            _rows.Add(bottomBorder);
            rows.Add(new RenderRegionRow(top + visibleHeight + 1, bottomBorder));
            _regions.Add(new RenderRegion("footer:input", 0, rows));

            // Calculate cursor position so the system caret (and IME) follows the visual cursor.
            // Left is: region left (0) + 1 for left border + prefix length + cursor column
            int prefixLengthForCursor = (viewport.CursorRow == 0 && viewport.VisibleLines.Count > 0) ? composer.PromptLabel.Length : 1;
            int cursorLeft = 0 + 1 + prefixLengthForCursor + viewport.CursorColumn;
            // Top is: top border row + 1 + cursor row index
            int cursorTop = top + 1 + viewport.CursorRow;

            return (cursorLeft, cursorTop);
        }

        public TerminalRenderFrame Build(int cursorLeft, int cursorTop, bool cursorVisible)
            => new(_width, _rows.ToArray(), _regions.ToArray(), cursorLeft, cursorTop, cursorVisible);
    }

    private readonly record struct StyledLine(string Text, ConsoleColor Color)
    {
        public static StyledLine Empty => new(string.Empty, ConsoleColor.DarkGray);
    }

    private readonly record struct InputVisualLine(int StartIndex, string Text);

    private readonly record struct InputViewport(
        IReadOnlyList<InputVisualLine> VisibleLines,
        int CursorRow,
        int CursorColumn,
        int FirstVisibleLineIndex,
        int TotalLineCount);

    private readonly record struct SurfacePanel(string RegionBase, TerminalPanelViewModel Panel);

    private static string Summarize(string text, int maxLength)
        => string.IsNullOrEmpty(text) || text.Length <= maxLength ? text : text[..Math.Max(1, maxLength - 1)] + "…";
}
