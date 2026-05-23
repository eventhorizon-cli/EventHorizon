using EventHorizon.Terminal;

namespace EventHorizon.Tests.Workspace;

public sealed class WorkspaceExplorerSnapshotBuilderTests : IDisposable
{
    private readonly string _root;

    public WorkspaceExplorerSnapshotBuilderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "eventhorizon-explorer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, ".eventhorizon", "sessions"));
        Directory.CreateDirectory(Path.Combine(_root, "src", "Nested"));
        Directory.CreateDirectory(Path.Combine(_root, "obj"));
        File.WriteAllText(Path.Combine(_root, "README.md"), "demo");
        File.WriteAllText(Path.Combine(_root, ".eventhorizon", "sessions", "hidden.json"), "{}");
        File.WriteAllText(Path.Combine(_root, "src", "Program.cs"), "class Program {}\n");
        File.WriteAllText(Path.Combine(_root, "src", "Nested", "Feature.cs"), "class Feature {}\n");
        File.WriteAllText(Path.Combine(_root, "obj", "ignored.tmp"), "ignored");
    }

    [Fact]
    public void Build_Shows_Tree_And_Highlights_Focused_Path()
    {
        IReadOnlyList<string> lines = WorkspaceExplorerSnapshotBuilder.Build(_root, "src/Program.cs", maxEntries: 20, maxDepth: 3);

        Assert.Contains(lines, static line => line.Contains("⌂", StringComparison.Ordinal));
        Assert.Contains(lines, static line => line.Contains("● src/", StringComparison.Ordinal));
        Assert.Contains(lines, static line => line.Contains("● Program.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, static line => line.Contains(".eventhorizon/", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, static line => line.Contains("obj/", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

