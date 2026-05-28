using System.Security.Cryptography;
using System.Text;

namespace EventHorizon.Workspace;

public sealed class WorkspaceSnapshotService
{
    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp",
        ".class",
        ".dll",
        ".dylib",
        ".exe",
        ".gif",
        ".ico",
        ".jar",
        ".jpeg",
        ".jpg",
        ".nupkg",
        ".o",
        ".pdf",
        ".png",
        ".so",
        ".tar",
        ".tif",
        ".tiff",
        ".zip",
    };

    private static readonly HashSet<string> IgnoredDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".idea",
        ".vs",
        "artifacts",
        "bin",
        "node_modules",
        "obj",
    };

    private readonly string _workspaceRoot;

    public WorkspaceSnapshotService(string workspaceRoot)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    public string WorkspaceRoot => _workspaceRoot;

    public async Task<WorkspaceSnapshot> CaptureAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, WorkspaceSnapshotEntry> entries = new(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in EnumerateWorkspaceFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var relativePath = Path.GetRelativePath(_workspaceRoot, filePath).Replace('\\', '/');
                var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
                var isBinary = IsBinary(filePath, bytes);
                var hash = Convert.ToHexString(SHA256.HashData(bytes));
                var text = isBinary ? null : Encoding.UTF8.GetString(bytes);
                entries[relativePath] = new WorkspaceSnapshotEntry(relativePath, isBinary, hash, text);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return new WorkspaceSnapshot(DateTimeOffset.UtcNow, entries);
    }

    private IEnumerable<string> EnumerateWorkspaceFiles()
        => Directory.EnumerateFiles(_workspaceRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path));

    private bool IsIgnoredPath(string fullPath)
    {
        var relativePath = Path.GetRelativePath(_workspaceRoot, fullPath);
        var segments = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(IgnoredDirectoryNames.Contains);
    }

    private static bool IsBinary(string filePath, byte[] bytes)
    {
        if (BinaryExtensions.Contains(Path.GetExtension(filePath)))
        {
            return true;
        }

        return bytes.Take(4096).Any(static value => value == 0);
    }
}


