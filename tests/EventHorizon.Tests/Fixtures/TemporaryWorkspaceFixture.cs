namespace EventHorizon.Tests.Fixtures;

/// <summary>
/// Provides a temporary workspace directory for tests with automatic cleanup.
/// </summary>
public sealed class TemporaryWorkspaceFixture : IDisposable
{
    public TemporaryWorkspaceFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), "eventhorizon-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }

    public string CreateSubdirectory(string name)
    {
        var path = Path.Combine(Root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    public string CreateFile(string relativePath, string content = "")
    {
        var fullPath = Path.Combine(Root, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(fullPath, content);
        return fullPath;
    }
}
