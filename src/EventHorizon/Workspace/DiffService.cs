namespace EventHorizon.Workspace;

public sealed class DiffService
{
    public IReadOnlyList<FileChange> GetChanges(WorkspaceSnapshot before, WorkspaceSnapshot after)
    {
        List<FileChange> changes = [];
        foreach (var path in before.Entries.Keys.Concat(after.Entries.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            var change = GetDiff(before, after, path);
            if (change is null)
            {
                continue;
            }

            changes.Add(new FileChange(change.Path, change.OldPath, change.Status, change.Additions, change.Deletions, change.Binary));
        }

        return changes;
    }

    public FileDiff? GetDiff(WorkspaceSnapshot before, WorkspaceSnapshot after, string path)
    {
        var normalizedPath = NormalizePath(path);
        var hasBefore = before.Entries.TryGetValue(normalizedPath, out var beforeEntry);
        var hasAfter = after.Entries.TryGetValue(normalizedPath, out var afterEntry);
        if (!hasBefore && !hasAfter)
        {
            return null;
        }

        if (hasBefore && hasAfter && string.Equals(beforeEntry!.Hash, afterEntry!.Hash, StringComparison.Ordinal))
        {
            return null;
        }

        var status = hasBefore
            ? hasAfter ? "modified" : "deleted"
            : "added";
        var binary = (beforeEntry?.Binary ?? false) || (afterEntry?.Binary ?? false);
        var oldText = beforeEntry?.Text;
        var newText = afterEntry?.Text;
        var (additions, deletions) = binary
            ? (0, 0)
            : CalculateLineChanges(oldText, newText);

        return new FileDiff(
            normalizedPath,
            OldPath: null,
            status,
            oldText,
            newText,
            ResolveLanguage(normalizedPath),
            binary,
            additions,
            deletions);
    }

    private static (int Additions, int Deletions) CalculateLineChanges(string? oldText, string? newText)
    {
        var oldLines = SplitLines(oldText);
        var newLines = SplitLines(newText);
        var commonCount = LongestCommonSubsequence(oldLines, newLines);
        return (newLines.Length - commonCount, oldLines.Length - commonCount);
    }

    private static int LongestCommonSubsequence(string[] oldLines, string[] newLines)
    {
        var lengths = new int[oldLines.Length + 1, newLines.Length + 1];
        for (var oldIndex = oldLines.Length - 1; oldIndex >= 0; oldIndex--)
        {
            for (var newIndex = newLines.Length - 1; newIndex >= 0; newIndex--)
            {
                lengths[oldIndex, newIndex] = string.Equals(oldLines[oldIndex], newLines[newIndex], StringComparison.Ordinal)
                    ? lengths[oldIndex + 1, newIndex + 1] + 1
                    : Math.Max(lengths[oldIndex + 1, newIndex], lengths[oldIndex, newIndex + 1]);
            }
        }

        return lengths[0, 0];
    }

    private static string ResolveLanguage(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".cs" => "csharp",
            ".csproj" => "xml",
            ".json" => "json",
            ".md" => "markdown",
            ".props" => "xml",
            ".razor" => "razor",
            ".sh" => "shell",
            ".sln" => "plaintext",
            ".slnx" => "xml",
            ".txt" => "plaintext",
            ".xml" => "xml",
            ".yml" => "yaml",
            ".yaml" => "yaml",
            _ => "plaintext",
        };

    private static string NormalizePath(string path)
        => path.Replace('\\', '/').TrimStart('/');

    private static string[] SplitLines(string? text)
        => string.IsNullOrEmpty(text)
            ? []
            : text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
}


