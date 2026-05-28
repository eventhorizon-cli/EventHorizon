using EventHorizon.Diff;

namespace EventHorizon.Tests.Diff;

public sealed class iffServiceTests
{
    [Fact]
    public void GetDiff_Returns_Modified_Text_File_With_Line_Counts()
    {
        DiffService service = new();
        var before = new WorkspaceSnapshot(
            DateTimeOffset.UtcNow,
            new Dictionary<string, WorkspaceSnapshotEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["src/Foo.cs"] = new("src/Foo.cs", false, "hash-old", "line1\nline2\nline3"),
            });
        var after = new WorkspaceSnapshot(
            DateTimeOffset.UtcNow,
            new Dictionary<string, WorkspaceSnapshotEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["src/Foo.cs"] = new("src/Foo.cs", false, "hash-new", "line1\nline2 changed\nline3\nline4"),
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
    public void GetChanges_Returns_Added_And_Deleted_Files()
    {
        DiffService service = new();
        var before = new WorkspaceSnapshot(
            DateTimeOffset.UtcNow,
            new Dictionary<string, WorkspaceSnapshotEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["src/Old.cs"] = new("src/Old.cs", false, "hash-old", "old"),
            });
        var after = new WorkspaceSnapshot(
            DateTimeOffset.UtcNow,
            new Dictionary<string, WorkspaceSnapshotEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["src/New.cs"] = new("src/New.cs", false, "hash-new", "new"),
            });

        var changes = service.GetChanges(before, after);

        Assert.Collection(
            changes,
            added =>
            {
                Assert.Equal("src/New.cs", added.Path);
                Assert.Equal("added", added.Status);
            },
            deleted =>
            {
                Assert.Equal("src/Old.cs", deleted.Path);
                Assert.Equal("deleted", deleted.Status);
            });
    }
}

