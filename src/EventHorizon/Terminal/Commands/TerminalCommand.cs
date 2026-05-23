namespace EventHorizon.Terminal.Commands;

public sealed record TerminalCommand(string RawText, string Name, string? Argument, bool IsSlashCommand)
{
    public static TerminalCommand Parse(string input)
    {
        string trimmed = input.Trim();
        if (!trimmed.StartsWith('/'))
        {
            return new TerminalCommand(trimmed, string.Empty, null, false);
        }

        string[] parts = trimmed.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return new TerminalCommand(
            trimmed,
            parts[0].ToLowerInvariant(),
            parts.Length > 1 ? parts[1] : null,
            true);
    }
}

