namespace EventHorizon.Terminal;

public sealed record TerminalTheme(
    string Name,
    string DefaultScheme,
    string ActiveScheme,
    string ErrorScheme,
    string SuccessScheme,
    string WarningScheme,
    string MutedScheme)
{
    public static TerminalTheme Midnight { get; } = new(
        "Midnight",
        "Base",
        "Dialog",
        "Error",
        "Base",
        "Dialog",
        "Menu");

    public static TerminalTheme Contrast { get; } = new(
        "Contrast",
        "Base",
        "TopLevel",
        "Error",
        "Base",
        "Dialog",
        "Menu");

    public static IReadOnlyList<TerminalTheme> Presets { get; } = [Midnight, Contrast];
}

