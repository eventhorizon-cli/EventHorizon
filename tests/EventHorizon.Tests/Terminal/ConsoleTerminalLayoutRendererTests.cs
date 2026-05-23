using EventHorizon.Terminal;

namespace EventHorizon.Tests.Terminal;

public sealed class ConsoleTerminalLayoutRendererTests
{
    [Fact]
    public void Input_Buffer_Change_Dirties_Only_Input_Region()
    {
        var before = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(input: string.Empty), 120, 36);
        var after = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(input: "hello"), 120, 36);

        var dirtyRegions = ConsoleTerminalLayoutRenderer.DiffRegions(before, after);

        Assert.Equal(["footer:input"], dirtyRegions);
    }

    [Fact]
    public void Status_Change_Dirties_Only_Primary_Status_Region()
    {
        var before = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(status: "Ready."), 120, 36);
        var after = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(status: "Streaming response…"), 120, 36);

        var dirtyRegions = ConsoleTerminalLayoutRenderer.DiffRegions(before, after);

        Assert.Equal(["footer:status:primary"], dirtyRegions);
    }

    [Fact]
    public void Conversation_Changes_Dirty_Only_Main_Viewport()
    {
        var before = ConsoleTerminalLayoutRenderer.BuildFrame(
            CreateViewModel(conversationLines:
            [
                "• 12:00:00 USER",
                "  summarize the repository",
                string.Empty,
                "• 12:00:01 ASSISTANT",
                "  working on it"
            ]),
            120,
            36);

        var after = ConsoleTerminalLayoutRenderer.BuildFrame(
            CreateViewModel(conversationLines:
            [
                "• 12:00:00 USER",
                "  summarize the repository",
                string.Empty,
                "~ 12:00:01 ASSISTANT",
                "  working on it... streamed delta",
                "  adding one more wrapped line to force a local repaint"
            ]),
            120,
            36);

        var dirtyRegions = ConsoleTerminalLayoutRenderer.DiffRegions(before, after);

        Assert.Equal(["surface:main:viewport"], dirtyRegions);
    }

    [Fact]
    public void Workbench_Uses_A_Single_Main_Surface()
    {
        var frame = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(), 120, 36);
        var regionKeys = frame.Regions.Select(static region => region.Key).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("surface:main:viewport", regionKeys);
        Assert.DoesNotContain("surface:sidebar:header", regionKeys);
        Assert.DoesNotContain("surface:inspector:viewport", regionKeys);
        Assert.DoesNotContain("surface:dock:viewport", regionKeys);
    }

    [Fact]
    public void Panel_Title_Changes_Dirty_Only_Main_Header()
    {
        var before = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(conversationTitle: "Session Thread"), 120, 36);
        var after = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(conversationTitle: "Session Thread *"), 120, 36);

        var dirtyRegions = ConsoleTerminalLayoutRenderer.DiffRegions(before, after);

        Assert.Equal(["surface:main:header"], dirtyRegions);
    }

    [Fact]
    public void Session_Tab_Changes_Do_Not_Affect_The_Single_Panel_Workbench()
    {
        var before = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(tabTitle: "Current"), 120, 36);
        var after = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(tabTitle: "Current *"), 120, 36);

        var dirtyRegions = ConsoleTerminalLayoutRenderer.DiffRegions(before, after);

        Assert.Empty(dirtyRegions);
    }

    [Fact]
    public void Command_Palette_Overlay_Adds_Overlay_Region()
    {
        var frame = ConsoleTerminalLayoutRenderer.BuildFrame(
            CreateViewModel(
                overlayOpen: true,
                overlayLines:
                [
                    "› /sidebar",
                    "  Switch the sidebar surface",
                    "  /sidebar overview"
                ]),
            120,
            36);

        Assert.Contains(frame.Regions, static region => region.Key == "overlay:command-palette");
    }

    [Fact]
    public void Command_Palette_Query_Changes_Dirty_Only_Overlay_Region()
    {
        var before = ConsoleTerminalLayoutRenderer.BuildFrame(
            CreateViewModel(overlayOpen: true, overlayQuery: "si", overlayLines: ["› /sidebar", "  Switch sidebar", "  /sidebar overview"]),
            120,
            36);
        var after = ConsoleTerminalLayoutRenderer.BuildFrame(
            CreateViewModel(overlayOpen: true, overlayQuery: "sess", overlayLines: ["› /sessions", "  Reload sessions", "  /sessions"]),
            120,
            36);

        var dirtyRegions = ConsoleTerminalLayoutRenderer.DiffRegions(before, after);

        Assert.Equal(["overlay:command-palette"], dirtyRegions);
    }

    [Fact]
    public void Long_Input_Uses_A_Bounded_Scrollable_Input_Box()
    {
        var longInput = string.Join(' ', Enumerable.Repeat("very-long-prompt-fragment", 40));
        var frame = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(input: longInput), 80, 24);
        var inputRegion = Assert.Single(frame.Regions, static region => region.Key == "footer:input");

        Assert.Equal(80, frame.Width);
        Assert.Equal(24, frame.Rows.Count);
        Assert.InRange(inputRegion.Rows.Count, 3, 8);
    }

    [Fact]
    public void Smaller_Terminal_Size_Rebalances_Layout_Without_Forcing_120x36()
    {
        var frame = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(), 80, 24);

        Assert.Equal(80, frame.Width);
        Assert.Equal(24, frame.Rows.Count);
        Assert.Contains(frame.Regions, static region => region.Key == "footer:input");
        Assert.Contains(frame.Regions, static region => region.Key == "surface:main:viewport");
    }

    [Fact]
    public void Input_Cursor_Renders_As_A_Visible_Bar_At_Buffer_End()
    {
        var frame = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(input: "hello"), 80, 24);
        var inputRegion = Assert.Single(frame.Regions, static region => region.Key == "footer:input");
        string inputText = string.Join("\n", inputRegion.Rows.SelectMany(static row => row.Row.Segments.Select(static segment => segment.Text)));

        Assert.False(frame.CursorVisible);
        Assert.Contains("hello|", inputText, StringComparison.Ordinal);
    }

    [Fact]
    public void Input_Cursor_Renders_At_The_Current_Insert_Position()
    {
        var frame = ConsoleTerminalLayoutRenderer.BuildFrame(CreateViewModel(input: "hello", inputCursorIndex: 3), 80, 24);
        var inputRegion = Assert.Single(frame.Regions, static region => region.Key == "footer:input");
        string inputText = string.Join("\n", inputRegion.Rows.SelectMany(static row => row.Row.Segments.Select(static segment => segment.Text)));

        Assert.Contains("hel|lo", inputText, StringComparison.Ordinal);
    }

    [Fact]
    public void Conversation_Scroll_Offset_Shows_Older_Transcript_Lines()
    {
        var frame = ConsoleTerminalLayoutRenderer.BuildFrame(
            CreateViewModel(
                conversationScrollOffset: 4,
                conversationLines:
                [
                    "• 12:00:00 USER",
                    "  line 01",
                    "• 12:00:01 ASSISTANT",
                    "  line 02",
                    "• 12:00:02 USER",
                    "  line 03",
                    "• 12:00:03 ASSISTANT",
                    "  line 04",
                    "• 12:00:04 USER",
                    "  line 05",
                    "• 12:00:05 ASSISTANT",
                    "  line 06",
                    "• 12:00:06 USER",
                    "  line 07",
                    "• 12:00:07 ASSISTANT",
                    "  line 08"
                ]),
            80,
            24);

        var viewport = Assert.Single(frame.Regions, static region => region.Key == "surface:main:viewport");
        string viewportText = string.Join("\n", viewport.Rows.SelectMany(static row => row.Row.Segments.Select(static segment => segment.Text))).TrimEnd();

        Assert.Contains("line 05", viewportText, StringComparison.Ordinal);
    }

    [Fact]
    public void Launchpad_Mode_Renders_A_Single_Full_Width_Surface()
    {
        var frame = ConsoleTerminalLayoutRenderer.BuildFrame(
            CreateViewModel(
                showLaunchpad: true,
                launchpadLines:
                [
                    "      /\\_/\\    @",
                    "  .-.(=^.^=).-. ",
                    "   \\_(__^__)_/  ",
                    string.Empty,
                    "Provider    openai",
                    "Status      [ready] OpenAI credentials detected"
                ]),
            100,
            28);

        var regionKeys = frame.Regions.Select(static region => region.Key).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("surface:launchpad:viewport", regionKeys);
        Assert.DoesNotContain("surface:main:viewport", regionKeys);
        Assert.Equal(100, frame.Width);
        Assert.Equal(28, frame.Rows.Count);
    }

    [Fact]
    public void Launchpad_Animation_Dirties_Only_Launchpad_Viewport()
    {
        var before = ConsoleTerminalLayoutRenderer.BuildFrame(
            CreateViewModel(showLaunchpad: true, launchpadLines: ["cat frame a", "status [ready]"], launchpadStatus: "Launchpad ready"),
            100,
            28);
        var after = ConsoleTerminalLayoutRenderer.BuildFrame(
            CreateViewModel(showLaunchpad: true, launchpadLines: ["cat frame b", "status [ready]"], launchpadStatus: "Launchpad ready"),
            100,
            28);

        var dirtyRegions = ConsoleTerminalLayoutRenderer.DiffRegions(before, after);

        Assert.Equal(["surface:launchpad:viewport"], dirtyRegions);
    }

    private static TerminalViewModel CreateViewModel(
        string? input = null,
        int? inputCursorIndex = null,
        string? status = null,
        IReadOnlyList<string>? conversationLines = null,
        IReadOnlyList<string>? dockLines = null,
        string conversationTitle = "Session Thread",
        string tabTitle = "Current",
        bool showLaunchpad = false,
        IReadOnlyList<string>? launchpadLines = null,
        string? launchpadStatus = null,
        bool overlayOpen = false,
        string overlayQuery = "",
        IReadOnlyList<string>? overlayLines = null,
        int conversationScrollOffset = 0)
        => new()
        {
            Title = "EventHorizon",
            Subtitle = showLaunchpad
                ? "Connection launchpad · confirm the active model before opening the full workbench"
                : "Minimal coding session",
            StatusIndicator = showLaunchpad ? "◌" : "●",
            HeaderContext = "Ready in repo · test/test-model",
            HeaderBadges = ["provider:test", "model:test-model", "workspace:repo", "session:abcd1234", showLaunchpad ? "mode:launchpad" : "mode:session"],
            Breadcrumbs = ["repo", "src", "Program.cs"],
            SessionTabs =
            [
                new TerminalTabViewModel { Id = "current", Title = tabTitle, Subtitle = "test/test-model", IsActive = true },
                new TerminalTabViewModel { Id = "saved", Title = "snapshot-1", Subtitle = "test/test-model", IsActive = false },
            ],
            Navigation = showLaunchpad
                ? []
                :
                [
                    new TerminalNavigationItemViewModel { PanelId = TerminalPanelCatalog.Explorer, Label = "Files", Shortcut = "⌃1" },
                    new TerminalNavigationItemViewModel { PanelId = TerminalPanelCatalog.Conversation, Label = "Chat", Shortcut = "⌃2", IsActive = true, Badge = "2" },
                    new TerminalNavigationItemViewModel { PanelId = TerminalPanelCatalog.Activity, Label = "Activity", Shortcut = "⌃3", Badge = "1" },
                    new TerminalNavigationItemViewModel { PanelId = TerminalPanelCatalog.Commands, Label = "Commands", Shortcut = "⌃4", Badge = "/" },
                    new TerminalNavigationItemViewModel { PanelId = TerminalPanelCatalog.Inspector, Label = "Inspect", Shortcut = "⌃5" },
                ],
            ShowLaunchpad = showLaunchpad,
            IsStreaming = false,
            LaunchpadSurface = new TerminalSurfaceViewModel
            {
                SurfaceId = "launchpad",
                IsVisible = showLaunchpad,
                Panel = new TerminalPanelViewModel
                {
                    PanelId = "launchpad",
                    Title = "Launchpad",
                    IsActive = true,
                    Lines = launchpadLines ??
                    [
                        "Welcome aboard.",
                        "Provider    test",
                        "Status      [ready] mock connection"
                    ]
                }
            },
            SidebarSurface = new TerminalSurfaceViewModel
            {
                SurfaceId = "sidebar",
                IsVisible = !showLaunchpad,
                Panel = new TerminalPanelViewModel
                {
                    PanelId = "overview",
                    Title = "Session Overview",
                    Lines =
                    [
                        "Focus        workspace root",
                        "Workspace    repo",
                        "Recent       12:00:00 [prompt] Submitted prompt",
                        "Usage        0/0/0",
                        "Model        test/test-model",
                        "Snapshots    none loaded",
                        "Actions      /help · /focus <path> · /save · /restore · /stats"
                    ]
                }
            },
            MainSurface = new TerminalSurfaceViewModel
            {
                SurfaceId = "main",
                IsVisible = !showLaunchpad,
                Panel = new TerminalPanelViewModel
                {
                    PanelId = TerminalPanelCatalog.Conversation,
                    Title = conversationTitle,
                    IsActive = true,
                    ScrollOffset = conversationScrollOffset,
                    Lines = conversationLines ??
                    [
                        "• 12:00:00 USER",
                        "  hello"
                    ]
                }
            },
            InspectorSurface = new TerminalSurfaceViewModel
            {
                SurfaceId = "inspector",
                IsVisible = !showLaunchpad,
                Panel = new TerminalPanelViewModel
                {
                    PanelId = TerminalPanelCatalog.Inspector,
                    Title = "Session Inspector",
                    Lines =
                    [
                        "Usage",
                        "Input      0",
                        "Output     0",
                        "Recent sessions",
                        "(none loaded)"
                    ]
                }
            },
            DockSurface = new TerminalSurfaceViewModel
            {
                SurfaceId = "dock",
                IsVisible = !showLaunchpad,
                Panel = new TerminalPanelViewModel
                {
                    PanelId = "activity-dock",
                    Title = "Recent Activity",
                    Lines = dockLines ??
                    [
                        "12:00:00 [prompt] Submitted prompt",
                        "  hello"
                    ]
                }
            },
            Overlay = new TerminalOverlayViewModel
            {
                IsOpen = overlayOpen,
                OverlayId = "overlay",
                Title = "Command Palette",
                Subtitle = "3 actions · Enter runs the selected item",
                Query = overlayQuery,
                SelectedIndex = 0,
                Lines = overlayLines ??
                [
                    "› /sidebar",
                    "  Switch the sidebar surface",
                    "  /sidebar overview"
                ]
            },
            Composer = new TerminalComposerViewModel
            {
                PromptLabel = showLaunchpad ? "▶" : "❯",
                Title = showLaunchpad ? "Quick start" : "Input",
                Buffer = input ?? string.Empty,
                CursorIndex = inputCursorIndex ?? (input ?? string.Empty).Length,
                Hint = "Type a prompt or use /help",
                Metadata = "←/→ move cursor · Home/End jump · Ctrl+K commands · PgUp/PgDn scroll transcript",
                UseMinimalChrome = false
            },
            StatusBar = new TerminalStatusBarViewModel
            {
                PrimaryText = launchpadStatus ?? status ?? "Ready.",
                SecondaryText = showLaunchpad
                    ? "Enter continue · type prompt now · /help commands"
                    : "Single panel mode · ←/→ cursor · Ctrl+K commands · PgUp/PgDn transcript"
            }
        };
}
