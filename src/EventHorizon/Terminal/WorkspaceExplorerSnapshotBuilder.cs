using EventHorizon.Terminal.Models;

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
        "node_modules",
    };

    public static IReadOnlyList<TerminalContextFile> Build(string workspaceRoot, string? focusedPath, int maxEntries = 28, int maxDepth = 2)
    {
        var root = Path.GetFullPath(workspaceRoot);
        List<TerminalContextFile> items =
        [
            new()
            {
                Path = Path.GetFileName(root),
                IsSelected = false,
                Description = "workspace root",
            },
        ];

        if (!Directory.Exists(root))
        {
            items.Add(new TerminalContextFile { Path = "workspace folder not found", Description = "missing" });
            return items;
        }

        var focusSegments = BuildFocusSegments(focusedPath);
        AppendEntries(root, 0, maxDepth, items, focusSegments, maxEntries);
        return items.Take(maxEntries).ToList();
    }

    private static void AppendEntries(
        string directory,
        int depth,
        int maxDepth,
        List<TerminalContextFile> items,
        HashSet<string> focusSegments,
        int maxEntries)
    {
        if (depth >= maxDepth || items.Count >= maxEntries)
        {
            return;
        }

        IEnumerable<string> directories = Directory.EnumerateDirectories(directory)
            .Where(static path => !HiddenDirectories.Contains(Path.GetFileName(path)))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var childDirectory in directories)
        {
            if (items.Count >= maxEntries)
            {
                return;
            }

            var name = Path.GetFileName(childDirectory) + "/";
            items.Add(new TerminalContextFile
            {
                Path = new string(' ', depth * 2) + name,
                IsSelected = focusSegments.Contains(Path.GetFileName(childDirectory)),
                Description = "directory",
            });
            AppendEntries(childDirectory, depth + 1, maxDepth, items, focusSegments, maxEntries);
        }

        foreach (var childFile in Directory.EnumerateFiles(directory).OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (items.Count >= maxEntries)
            {
                return;
            }

            var fileName = Path.GetFileName(childFile);
            items.Add(new TerminalContextFile
            {
                Path = new string(' ', depth * 2) + fileName,
                IsSelected = focusSegments.Contains(fileName),
                Description = "file",
                SizeBytes = new FileInfo(childFile).Length,
            });
        }
    }

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

