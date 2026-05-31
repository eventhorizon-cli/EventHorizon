namespace EventHorizon.Workspace.Diff;

public sealed class DiffService : IDiffService
{
    private const int MaxInlineDiffCharacters = 200_000;

    public IReadOnlyList<FileChange> GetChanges(WorkspaceSnapshot before, WorkspaceSnapshot after)
        => GetDiffs(before, after)
            .Select(static change => new FileChange(change.Path, change.OldPath, change.Status, change.Additions, change.Deletions, change.Binary))
            .ToArray();

    public FileDiff? GetDiff(WorkspaceSnapshot before, WorkspaceSnapshot after, string path)
        => GetDiffs(before, after)
            .FirstOrDefault(change =>
                string.Equals(change.Path, NormalizePath(path), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(change.OldPath, NormalizePath(path), StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<FileDiff> GetDiffs(WorkspaceSnapshot before, WorkspaceSnapshot after)
    {
        List<FileDiff> changes = [];
        var addedPaths = after.Entries.Keys.Except(before.Entries.Keys, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var deletedPaths = before.Entries.Keys.Except(after.Entries.Keys, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var renamed in MatchRenames(before, after, deletedPaths, addedPaths))
        {
            changes.Add(renamed);
        }

        foreach (var sharedPath in before.Entries.Keys.Intersect(after.Entries.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            var diff = CreateDiff(sharedPath, null, before.Entries[sharedPath], after.Entries[sharedPath]);
            if (diff is not null)
            {
                changes.Add(diff);
            }
        }

        foreach (var addedPath in addedPaths.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            var diff = CreateDiff(addedPath, null, null, after.Entries[addedPath]);
            if (diff is not null)
            {
                changes.Add(diff);
            }
        }

        foreach (var deletedPath in deletedPaths.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            var diff = CreateDiff(deletedPath, null, before.Entries[deletedPath], null);
            if (diff is not null)
            {
                changes.Add(diff);
            }
        }

        return changes
            .OrderBy(static change => change.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static change => change.OldPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public FileDiff? CreateDiff(string path, string? oldPath, FileSnapshot? oldSnapshot, FileSnapshot? newSnapshot)
    {
        path = NormalizePath(path);
        oldPath = string.IsNullOrWhiteSpace(oldPath) ? null : NormalizePath(oldPath);
        if (oldSnapshot is null && newSnapshot is null)
        {
            return null;
        }

        if (oldSnapshot is not null &&
            newSnapshot is not null &&
            oldPath is null &&
            string.Equals(oldSnapshot.ContentHash, newSnapshot.ContentHash, StringComparison.Ordinal))
        {
            return null;
        }

        var status = ResolveStatus(path, oldPath, oldSnapshot, newSnapshot);
        var binary = (oldSnapshot?.IsBinary ?? false) || (newSnapshot?.IsBinary ?? false);
        var oldText = oldSnapshot?.Content;
        var newText = newSnapshot?.Content;
        var (additions, deletions) = binary
            ? (0, 0)
            : CalculateLineChanges(oldText, newText);

        if (!binary && (oldText?.Length ?? 0) + (newText?.Length ?? 0) > MaxInlineDiffCharacters)
        {
            oldText = null;
            newText = null;
        }

        return new FileDiff(
            path,
            oldPath,
            status,
            oldText,
            newText,
            ResolveLanguage(path),
            binary,
            additions,
            deletions);
    }

    private IEnumerable<FileDiff> MatchRenames(WorkspaceSnapshot before, WorkspaceSnapshot after, HashSet<string> deletedPaths, HashSet<string> addedPaths)
    {
        var addedByHash = addedPaths
            .Select(path => after.Entries[path])
            .GroupBy(static snapshot => snapshot.ContentHash, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => new Queue<FileSnapshot>(group), StringComparer.Ordinal);

        foreach (var deletedPath in deletedPaths.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase).ToArray())
        {
            var deletedSnapshot = before.Entries[deletedPath];
            if (!addedByHash.TryGetValue(deletedSnapshot.ContentHash, out var matches) || matches.Count == 0)
            {
                continue;
            }

            var addedSnapshot = matches.Dequeue();
            addedPaths.Remove(addedSnapshot.Path);
            deletedPaths.Remove(deletedPath);
            yield return CreateDiff(addedSnapshot.Path, deletedPath, deletedSnapshot, addedSnapshot)!;
        }
    }

    private static string ResolveStatus(string path, string? oldPath, FileSnapshot? oldSnapshot, FileSnapshot? newSnapshot)
    {
        if (oldSnapshot is null)
        {
            return "added";
        }

        if (newSnapshot is null)
        {
            return "deleted";
        }

        if (!string.IsNullOrWhiteSpace(oldPath) && !string.Equals(path, oldPath, StringComparison.OrdinalIgnoreCase))
        {
            return "renamed";
        }

        return "modified";
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
            ".css" => "css",
            ".html" => "html",
            ".js" => "javascript",
            ".json" => "json",
            ".jsx" => "javascript",
            ".md" => "markdown",
            ".props" => "xml",
            ".razor" => "razor",
            ".sh" => "shell",
            ".sln" => "plaintext",
            ".slnx" => "xml",
            ".sql" => "sql",
            ".ts" => "typescript",
            ".tsx" => "typescript",
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
