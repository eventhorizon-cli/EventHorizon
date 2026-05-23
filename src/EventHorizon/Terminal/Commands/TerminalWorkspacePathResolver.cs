namespace EventHorizon.Terminal.Commands;

internal static class TerminalWorkspacePathResolver
{
    public static string ResolveInsideWorkspace(string workspaceRoot, string input)
    {
        string root = Path.GetFullPath(workspaceRoot);
        string candidate = Path.IsPathRooted(input)
            ? Path.GetFullPath(input)
            : Path.GetFullPath(Path.Combine(root, input));

        if (!candidate.StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The focus target escapes the configured workspace root.");
        }

        if (!File.Exists(candidate) && !Directory.Exists(candidate))
        {
            throw new InvalidOperationException($"The focus target '{input}' does not exist.");
        }

        return Path.GetRelativePath(root, candidate).Replace(Path.DirectorySeparatorChar, '/');
    }
}

