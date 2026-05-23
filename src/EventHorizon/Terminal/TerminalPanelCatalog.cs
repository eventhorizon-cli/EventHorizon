namespace EventHorizon.Terminal;

public static class TerminalPanelCatalog
{
    public const string Explorer = "explorer";
    public const string Conversation = "conversation";
    public const string Activity = "activity";
    public const string Commands = "commands";
    public const string Inspector = "inspector";

    public static IReadOnlyList<string> Ordered { get; } =
    [
        Explorer,
        Conversation,
        Activity,
        Commands,
        Inspector,
    ];

    public static bool IsKnown(string panelId)
        => Ordered.Contains(panelId, StringComparer.OrdinalIgnoreCase);
}

