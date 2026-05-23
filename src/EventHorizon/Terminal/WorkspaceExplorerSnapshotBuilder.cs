namespace EventHorizon.Terminal;

public static class WorkspaceExplorerSnapshotBuilder
{
    private static readonly HashSet<string> HiddenDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".eventhorizon",
        ".idea",
        ".vs",
        "bin",
        "obj",
        "node_modules"
    };

    public static IReadOnlyList<string> Build(string workspaceRoot, string? focusedPath, int maxEntries = 28, int maxDepth = 2)
    {
        string root = Path.GetFullPath(workspaceRoot);
        List<string> lines = [$"⌂ {Path.GetFileName(root)}"];
        HashSet<string> focusSegments = BuildFocusSegments(focusedPath);

        if (!Directory.Exists(root))
        {
            lines.Add("! workspace folder not found");
            return lines;
        }

        AppendEntries(root, depth: 0, maxDepth, lines, focusSegments, maxEntries);

        if (lines.Count == 1)
        {
            lines.Add("(empty workspace)");
        }

        return lines.Take(maxEntries).ToList();
    }

    private static void AppendEntries(string directory, int depth, int maxDepth, List<string> lines, HashSet<string> focusSegments, int maxEntries)
    {
        if (depth >= maxDepth || lines.Count >= maxEntries)
        {
            return;
        }

        IEnumerable<string> directories = Directory.EnumerateDirectories(directory)
            .Where(static path => !HiddenDirectories.Contains(Path.GetFileName(path)))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase);

        foreach (string childDirectory in directories)
        {
            if (lines.Count >= maxEntries)
            {
                return;
            }

            string name = Path.GetFileName(childDirectory) + "/";
            lines.Add(FormatEntry(depth, name, focusSegments.Contains(Path.GetFileName(childDirectory))));
            AppendEntries(childDirectory, depth + 1, maxDepth, lines, focusSegments, maxEntries);
        }

        foreach (string childFile in Directory.EnumerateFiles(directory).OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (lines.Count >= maxEntries)
            {
                return;
            }

            string fileName = Path.GetFileName(childFile);
            lines.Add(FormatEntry(depth, fileName, focusSegments.Contains(fileName)));
        }
    }

    private static string FormatEntry(int depth, string name, bool isFocused)
        => $"{new string(' ', depth * 2)}{(isFocused ? '●' : '•')} {name}";

    private static HashSet<string> BuildFocusSegments(string? focusedPath)
    {
        if (string.IsNullOrWhiteSpace(focusedPath))
        {
            return [];
        }

        return focusedPath
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

