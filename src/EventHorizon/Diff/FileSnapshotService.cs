using System.Security.Cryptography;
using System.Text;

namespace EventHorizon.Diff;

public sealed class FileSnapshotService
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

    public FileSnapshotService(string workspaceRoot)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    public string WorkspaceRoot => _workspaceRoot;

    public FileSnapshot? CaptureFile(string path)
    {
        var resolvedPath = ResolvePath(path);
        if (!File.Exists(resolvedPath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(resolvedPath);
        var relativePath = NormalizePath(resolvedPath);
        var isBinary = IsBinary(resolvedPath, bytes);

        return new FileSnapshot(
            relativePath,
            isBinary ? null : Encoding.UTF8.GetString(bytes),
            isBinary ? bytes : null,
            Convert.ToHexString(SHA256.HashData(bytes)),
            isBinary,
            DateTimeOffset.UtcNow);
    }

    public WorkspaceSnapshot CaptureWorkspace()
    {
        Dictionary<string, FileSnapshot> entries = new(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in Directory.EnumerateFiles(_workspaceRoot, "*", SearchOption.AllDirectories).Where(path => !IsIgnoredPath(path)))
        {
            try
            {
                var snapshot = CaptureFile(filePath);
                if (snapshot is not null)
                {
                    entries[snapshot.Path] = snapshot;
                }
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

    public string NormalizePath(string path)
    {
        var resolvedPath = ResolvePath(path);
        return Path.GetRelativePath(_workspaceRoot, resolvedPath).Replace('\\', '/');
    }

    public string ResolvePath(string path)
    {
        var candidate = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(_workspaceRoot, path));
        var rootWithSeparator = _workspaceRoot.EndsWith(Path.DirectorySeparatorChar)
            ? _workspaceRoot
            : _workspaceRoot + Path.DirectorySeparatorChar;

        if (!candidate.Equals(_workspaceRoot, StringComparison.Ordinal) &&
            !candidate.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The requested path escapes the configured workspace root.");
        }

        return candidate;
    }

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

