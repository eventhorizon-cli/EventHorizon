using EventHorizon.Configuration;
using EventHorizon.Pricing;
using EventHorizon.Terminal.Surfaces;

namespace EventHorizon.Terminal;

public sealed class TerminalWorkbenchComposer
{
    private readonly IReadOnlyDictionary<string, ITerminalSurfaceBuilder> _surfaceBuilders;

    public TerminalWorkbenchComposer(IEnumerable<ITerminalSurfaceBuilder> surfaceBuilders)
    {
        _surfaceBuilders = surfaceBuilders.ToDictionary(static builder => builder.SurfaceId, StringComparer.OrdinalIgnoreCase);
    }

    public TerminalViewModel Compose(
        AppOptions options,
        TerminalConversationState state,
        SessionUsageTracker usageTracker,
        string status,
        bool isStreaming,
        string? assistantPreview = null,
        int animationFrameIndex = 0)
    {
        var transcript = BuildTranscript(state, assistantPreview);
        var model = options.Provider.Model ?? options.Provider.Deployment ?? "unknown-model";
        var paletteItems = TerminalCommandCatalog.BuildPaletteItems(state.CommandHistory, state.SavedSessions, state.FocusedPath, state.SidebarMode);
        TerminalSurfaceBuildContext context = new()
        {
            Options = options,
            State = state,
            UsageTracker = usageTracker,
            Transcript = transcript,
            PaletteItems = paletteItems,
            Model = model,
            IsStreaming = isStreaming,
            AnimationFrameIndex = animationFrameIndex,
        };

        var showLaunchpad = state.ShowLaunchpad && !isStreaming;
        var launchpad = BuildSurface("launchpad", context);
        var sidebar = BuildSurface("sidebar", context);
        var main = BuildSurface("main", context);
        var inspector = BuildSurface("inspector", context);
        var dock = BuildSurface("dock", context);
        var overlaySurface = BuildSurface("overlay", context);
        var filteredPaletteItems = TerminalCommandCatalog.FilterPaletteItems(paletteItems, state.CommandPalette.Query);

        return new TerminalViewModel
        {
            Title = "EventHorizon",
            Subtitle = showLaunchpad
                ? "Connection launchpad · validate provider state before entering the workbench"
                : "Minimal coding session",
            StatusIndicator = BuildStatusIndicator(isStreaming, showLaunchpad, animationFrameIndex),
            HeaderContext = BuildHeaderContext(options, state, model, isStreaming, showLaunchpad),
            HeaderBadges = BuildHeaderBadges(options, state, model, isStreaming, showLaunchpad),
            Breadcrumbs = BuildBreadcrumbs(options, state),
            SessionTabs = BuildSessionTabs(options, state, model),
            Navigation = showLaunchpad ? [] : BuildNavigation(state),
            ShowLaunchpad = showLaunchpad,
            IsStreaming = isStreaming,
            LaunchpadSurface = launchpad,
            SidebarSurface = sidebar,
            MainSurface = main,
            InspectorSurface = inspector,
            DockSurface = dock,
            Overlay = new TerminalOverlayViewModel
            {
                IsOpen = state.CommandPalette.IsOpen,
                OverlayId = overlaySurface.SurfaceId,
                Title = overlaySurface.Panel.Title,
                Subtitle = filteredPaletteItems.Count == 0
                    ? "No matching actions"
                    : $"{filteredPaletteItems.Count} actions · Enter runs the selected item",
                Query = state.CommandPalette.Query,
                SelectedIndex = filteredPaletteItems.Count == 0 ? 0 : Math.Clamp(state.CommandPalette.SelectedIndex, 0, filteredPaletteItems.Count - 1),
                Lines = overlaySurface.Panel.Lines,
            },
            Composer = new TerminalComposerViewModel
            {
                PromptLabel = isStreaming ? "…" : (showLaunchpad ? ">" : ">"),
                Title = showLaunchpad ? "Quick start" : "Input",
                Buffer = state.PendingInput,
                CursorIndex = state.PendingInputCursorIndex,
                Hint = BuildComposerHint(options, state, isStreaming, showLaunchpad),
                Metadata = BuildComposerMetadata(state, isStreaming, showLaunchpad),
                UseMinimalChrome = false,
            },
            StatusBar = new TerminalStatusBarViewModel
            {
                PrimaryText = status,
                SecondaryText = BuildSecondaryStatus(state, isStreaming, filteredPaletteItems.Count),
            },
        };
    }

    private TerminalSurfaceViewModel BuildSurface(string surfaceId, TerminalSurfaceBuildContext context)
    {
        if (_surfaceBuilders.TryGetValue(surfaceId, out ITerminalSurfaceBuilder? builder))
        {
            return builder.Build(context);
        }

        return new TerminalSurfaceViewModel
        {
            SurfaceId = surfaceId,
            Panel = new TerminalPanelViewModel
            {
                PanelId = surfaceId,
                Title = surfaceId,
                Lines = [$"Missing surface builder for '{surfaceId}'."],
            },
        };
    }

    private static IReadOnlyList<TerminalMessage> BuildTranscript(TerminalConversationState state, string? assistantPreview)
    {
        List<TerminalMessage> transcript = [.. state.Transcript];
        if (!string.IsNullOrWhiteSpace(assistantPreview))
        {
            transcript.Add(new TerminalMessage
            {
                Role = "assistant",
                Text = assistantPreview,
                Timestamp = DateTimeOffset.UtcNow,
                IsStreamingPreview = true,
            });
        }

        return transcript;
    }

    private static IReadOnlyList<string> BuildHeaderBadges(AppOptions options, TerminalConversationState state, string model, bool isStreaming, bool showLaunchpad)
    {
        List<string> badges =
        [
            $"provider:{options.Provider.Type}",
            $"model:{model}",
            $"workspace:{Path.GetFileName(options.WorkspaceRoot)}",
            $"session:{state.SessionId[..Math.Min(8, state.SessionId.Length)]}",
            showLaunchpad ? "mode:launchpad" : (isStreaming ? "mode:streaming" : "mode:session"),
            $"sidebar:{state.SidebarMode}",
        ];

        if (!string.IsNullOrWhiteSpace(state.FocusedPath))
        {
            badges.Add($"focus:{Summarize(state.FocusedPath, 24)}");
        }

        return badges;
    }

    private static IReadOnlyList<string> BuildBreadcrumbs(AppOptions options, TerminalConversationState state)
    {
        List<string> breadcrumbs = [Path.GetFileName(options.WorkspaceRoot)];
        if (string.IsNullOrWhiteSpace(state.FocusedPath))
        {
            breadcrumbs.Add("workspace root");
        }
        else
        {
            breadcrumbs.AddRange(state.FocusedPath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries));
        }

        if (state.CommandPalette.IsOpen)
        {
            breadcrumbs.Add("palette");
        }

        return breadcrumbs;
    }

    private static IReadOnlyList<TerminalTabViewModel> BuildSessionTabs(AppOptions options, TerminalConversationState state, string model)
    {
        List<TerminalTabViewModel> tabs =
        [
            new()
            {
                Id = state.SessionId,
                Title = "Current",
                Subtitle = $"{options.Provider.Type}/{model}",
                IsActive = true,
            },
        ];

        tabs.AddRange(state.SavedSessions
            .Where(session => !string.Equals(session.Id, state.SessionId, StringComparison.OrdinalIgnoreCase))
            .Take(4)
            .Select(session => new TerminalTabViewModel
            {
                Id = session.Id,
                Title = session.Name,
                Subtitle = $"{session.ProviderType}/{session.Model}",
                IsActive = false,
            }));

        return tabs;
    }

    private static IReadOnlyList<TerminalNavigationItemViewModel> BuildNavigation(TerminalConversationState state)
        =>
        [
            new() { PanelId = TerminalSidebarModeCatalog.Overview, Label = "Overview", Shortcut = "⌃0", Badge = string.Empty, IsActive = string.Equals(state.SidebarMode, TerminalSidebarModeCatalog.Overview, StringComparison.OrdinalIgnoreCase) },
            new() { PanelId = TerminalSidebarModeCatalog.Files, Label = "Files", Shortcut = "⌃1", Badge = string.IsNullOrWhiteSpace(state.FocusedPath) ? string.Empty : "focus", IsActive = string.Equals(state.SidebarMode, TerminalSidebarModeCatalog.Files, StringComparison.OrdinalIgnoreCase) },
            new() { PanelId = TerminalPanelCatalog.Conversation, Label = "Chat", Shortcut = "⌃2", Badge = state.Transcript.Count == 0 ? string.Empty : Math.Min(9, state.Transcript.Count).ToString(), IsActive = string.Equals(state.ActivePanelId, TerminalPanelCatalog.Conversation, StringComparison.OrdinalIgnoreCase) },
            new() { PanelId = TerminalSidebarModeCatalog.Activity, Label = "Activity", Shortcut = "⌃3", Badge = state.ActivityFeed.Count == 0 ? string.Empty : Math.Min(9, state.ActivityFeed.Count).ToString(), IsActive = string.Equals(state.SidebarMode, TerminalSidebarModeCatalog.Activity, StringComparison.OrdinalIgnoreCase) },
            new() { PanelId = TerminalSidebarModeCatalog.Commands, Label = "Command", Shortcut = "⌃4", Badge = state.CommandPalette.IsOpen ? "modal" : "/", IsActive = string.Equals(state.SidebarMode, TerminalSidebarModeCatalog.Commands, StringComparison.OrdinalIgnoreCase) || state.CommandPalette.IsOpen },
            new() { PanelId = TerminalSidebarModeCatalog.Sessions, Label = "Sessions", Shortcut = "/sessions", Badge = state.SavedSessions.Count == 0 ? string.Empty : Math.Min(9, state.SavedSessions.Count).ToString(), IsActive = string.Equals(state.SidebarMode, TerminalSidebarModeCatalog.Sessions, StringComparison.OrdinalIgnoreCase) },
            new() { PanelId = TerminalSidebarModeCatalog.Errors, Label = "Errors", Shortcut = "/sidebar errors", Badge = state.ErrorFeed.Count == 0 ? string.Empty : Math.Min(9, state.ErrorFeed.Count).ToString(), IsActive = string.Equals(state.SidebarMode, TerminalSidebarModeCatalog.Errors, StringComparison.OrdinalIgnoreCase) },
            new() { PanelId = TerminalPanelCatalog.Inspector, Label = "Inspect", Shortcut = "⌃5", Badge = string.Empty, IsActive = string.Equals(state.ActivePanelId, TerminalPanelCatalog.Inspector, StringComparison.OrdinalIgnoreCase) },
        ];

    private static string BuildComposerHint(AppOptions options, TerminalConversationState state, bool isStreaming, bool showLaunchpad)
    {
        if (state.CommandPalette.IsOpen)
        {
            return "Command palette is open. Type to filter actions, use ↑/↓ to move, Enter to execute, and Esc to close.";
        }

        if (isStreaming)
        {
            return "Streaming response in progress. New output will keep appending in the main panel.";
        }

        if (showLaunchpad)
        {
            return TerminalLaunchpad.DescribeConnection(options).IsReady
                ? "Press Enter to open the full workbench, or type a prompt now to begin immediately."
                : "Finish configuring the provider connection first, then open the workbench or restart with a config preset.";
        }

        return string.IsNullOrWhiteSpace(state.FocusedPath)
            ? "Describe a change, paste an error, or ask for a review. ←/→ moves the cursor and Ctrl+K opens commands."
            : $"Focused on {state.FocusedPath}. Ask for edits, tests, refactors, or press Ctrl+K for commands.";
    }

    private static string BuildComposerMetadata(TerminalConversationState state, bool isStreaming, bool showLaunchpad)
    {
        if (state.CommandPalette.IsOpen)
        {
            return $"palette query: {(string.IsNullOrWhiteSpace(state.CommandPalette.Query) ? "all actions" : state.CommandPalette.Query)}";
        }

        if (showLaunchpad)
        {
            return "Enter continue · type prompt now · /help palette · /sidebar modes available later";
        }

        if (isStreaming)
        {
            return "streaming · Ctrl+L repaint";
        }

        return !string.IsNullOrWhiteSpace(state.PendingInputMetadata)
            ? state.PendingInputMetadata
            : "←/→ move cursor · Home/End jump · Ctrl+K commands · PgUp/PgDn scroll transcript";
    }

    private static string BuildStatusIndicator(bool isStreaming, bool showLaunchpad, int animationFrameIndex)
    {
        if (showLaunchpad)
        {
            return "◌";
        }

        if (!isStreaming)
        {
            return "●";
        }

        string[] frames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
        return frames[Math.Abs(animationFrameIndex) % frames.Length];
    }

    private static string BuildHeaderContext(AppOptions options, TerminalConversationState state, string model, bool isStreaming, bool showLaunchpad)
    {
        if (showLaunchpad)
        {
            return "Validate the current provider connection, then enter the session workbench.";
        }

        string workspace = Path.GetFileName(options.WorkspaceRoot);
        if (state.CommandPalette.IsOpen)
        {
            return $"Palette open in {workspace} · {options.Provider.Type}/{model} · run commands, restore sessions, or switch sidebar content";
        }

        if (isStreaming)
        {
            return $"Streaming in {workspace} · {options.Provider.Type}/{model}";
        }

        return string.IsNullOrWhiteSpace(state.FocusedPath)
            ? $"Ready in {workspace} · {options.Provider.Type}/{model}"
            : $"Focused on {state.FocusedPath} · {options.Provider.Type}/{model}";
    }

    private static string BuildSecondaryStatus(TerminalConversationState state, bool isStreaming, int filteredPaletteCount)
    {
        if (state.CommandPalette.IsOpen)
        {
            return filteredPaletteCount == 0
                ? "No palette matches · Esc closes the modal"
                : $"{filteredPaletteCount} palette items · Enter runs selected · Esc closes · ↑/↓ move";
        }

        return isStreaming
            ? "Live stream · output updates in place"
            : "Single panel mode · ←/→ cursor · Ctrl+K commands · PgUp/PgDn transcript";
    }

    private static string Summarize(string text, int maxLength)
        => string.IsNullOrWhiteSpace(text) || text.Length <= maxLength ? text : text[..Math.Max(1, maxLength - 1)] + "…";
}

