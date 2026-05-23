namespace EventHorizon.Terminal;

public static class TerminalSidebarModeCatalog
{
    public const string Overview = "overview";
    public const string Files = "files";
    public const string Activity = "activity";
    public const string Commands = "commands";
    public const string Sessions = "sessions";
    public const string Errors = "errors";

    public static IReadOnlyList<string> Ordered { get; } =
    [
        Overview,
        Files,
        Activity,
        Commands,
        Sessions,
        Errors,
    ];

    public static bool IsKnown(string mode)
        => Ordered.Contains(mode, StringComparer.OrdinalIgnoreCase);
}

