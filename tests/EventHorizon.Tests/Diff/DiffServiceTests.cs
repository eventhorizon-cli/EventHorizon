using System.Security.Cryptography;
using System.Text;
using EventHorizon.Workspace.Diff;

namespace EventHorizon.Tests.Diff;

public sealed class DiffServiceTests
{
    [Fact]
    public void GetDiff_Returns_Modified_Text_File_With_Line_Counts()
    {
        DiffService service = new();
        var before = new WorkspaceSnapshot(
            DateTimeOffset.UtcNow,
            new Dictionary<string, FileSnapshot>(StringComparer.OrdinalIgnoreCase)
            {
                ["src/Foo.cs"] = CreateSnapshot("src/Foo.cs", "line1\nline2\nline3"),
            });
        var after = new WorkspaceSnapshot(
            DateTimeOffset.UtcNow,
            new Dictionary<string, FileSnapshot>(StringComparer.OrdinalIgnoreCase)
            {
                ["src/Foo.cs"] = CreateSnapshot("src/Foo.cs", "line1\nline2 changed\nline3\nline4"),
            });

        var diff = service.GetDiff(before, after, "src/Foo.cs");

        Assert.NotNull(diff);
        Assert.Equal("modified", diff.Status);
        Assert.Equal("csharp", diff.Language);
        Assert.False(diff.Binary);
        Assert.Equal(2, diff.Additions);
        Assert.Equal(1, diff.Deletions);
    }

    [Fact]
    public void GetChanges_Detects_Renames_From_NonGit_Snapshots()
    {
        DiffService service = new();
        var before = new WorkspaceSnapshot(
            DateTimeOffset.UtcNow,
            new Dictionary<string, FileSnapshot>(StringComparer.OrdinalIgnoreCase)
            {
                ["src/Old.cs"] = CreateSnapshot("src/Old.cs", "class Old {}\n"),
            });
        var after = new WorkspaceSnapshot(
            DateTimeOffset.UtcNow,
            new Dictionary<string, FileSnapshot>(StringComparer.OrdinalIgnoreCase)
            {
                ["src/New.cs"] = CreateSnapshot("src/New.cs", "class Old {}\n"),
            });

        var changes = service.GetChanges(before, after);

        var change = Assert.Single(changes);
        Assert.Equal("renamed", change.Status);
        Assert.Equal("src/New.cs", change.Path);
        Assert.Equal("src/Old.cs", change.OldPath);
    }

    private static FileSnapshot CreateSnapshot(string path, string content)
        => new(
            path,
            content,
            null,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))),
            false,
            DateTimeOffset.UtcNow);
}
