namespace EventHorizon.Terminal;

public sealed class TerminalViewModel
{
    public string Title { get; set; } = "EventHorizon";
    public string Subtitle { get; set; } = "Terminal-native coding session";
    public string StatusIndicator { get; set; } = "●";
    public string HeaderContext { get; set; } = string.Empty;
    public IReadOnlyList<string> HeaderBadges { get; set; } = [];
    public IReadOnlyList<string> Breadcrumbs { get; set; } = [];
    public IReadOnlyList<TerminalTabViewModel> SessionTabs { get; set; } = [];
    public IReadOnlyList<TerminalNavigationItemViewModel> Navigation { get; set; } = [];
    public bool ShowLaunchpad { get; set; }
    public bool IsStreaming { get; set; }
    public TerminalSurfaceViewModel LaunchpadSurface { get; set; } = new()
    {
        SurfaceId = "launchpad",
        Panel = new TerminalPanelViewModel { PanelId = "launchpad", Title = "Launchpad" },
    };
    public TerminalSurfaceViewModel SidebarSurface { get; set; } = new()
    {
        SurfaceId = "sidebar",
        Panel = new TerminalPanelViewModel { PanelId = "overview", Title = "Session Overview" },
    };
    public TerminalSurfaceViewModel MainSurface { get; set; } = new()
    {
        SurfaceId = "main",
        Panel = new TerminalPanelViewModel { PanelId = TerminalPanelCatalog.Conversation, Title = "Session Thread" },
    };
    public TerminalSurfaceViewModel InspectorSurface { get; set; } = new()
    {
        SurfaceId = "inspector",
        Panel = new TerminalPanelViewModel { PanelId = TerminalPanelCatalog.Inspector, Title = "Inspector" },
    };
    public TerminalSurfaceViewModel DockSurface { get; set; } = new()
    {
        SurfaceId = "dock",
        Panel = new TerminalPanelViewModel { PanelId = "overview-dock", Title = "Quick Context" },
    };
    public TerminalOverlayViewModel Overlay { get; set; } = new();
    public TerminalComposerViewModel Composer { get; set; } = new();
    public TerminalStatusBarViewModel StatusBar { get; set; } = new();
}


