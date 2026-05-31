using EventHorizon.Workspace;
using EventHorizon.Workspace.Diff;

namespace EventHorizon.Tests.Diff;

public sealed class FileStateTrackerTests : IDisposable
{
    private readonly string _workspaceRoot;
    private readonly WorkspaceContext _workspaceContext;
    private readonly FileSnapshotService _fileSnapshotService;
    private readonly DiffService _diffService;

    public FileStateTrackerTests()
    {
        _workspaceRoot = Path.Combine(Path.GetTempPath(), "eventhorizon-file-state-tracker-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspaceRoot);
        _workspaceContext = new WorkspaceContext(_workspaceRoot);
        _fileSnapshotService = new FileSnapshotService(_workspaceContext);
        _diffService = new DiffService();
    }

    [Fact]
    public void Multiple_Modifications_Keep_First_Baseline_And_Final_Text()
    {
        var filePath = Path.Combine(_workspaceRoot, "src", "Foo.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, "line1\nline2\n");

        FileStateTracker tracker = new("run_1", "session_1", _fileSnapshotService, _diffService);

        tracker.CaptureBaseline(filePath);
        File.WriteAllText(filePath, "line1\nline2 updated\n");
        tracker.CaptureCurrent(filePath);

        File.WriteAllText(filePath, "line1\nline2 updated\nline3\n");
        tracker.CaptureCurrent(filePath);

        var diff = tracker.GetDiff("src/Foo.cs");

        Assert.NotNull(diff);
        Assert.Equal("modified", diff.Status);
        Assert.Equal("line1\nline2\n", diff.OldText);
        Assert.Equal("line1\nline2 updated\nline3\n", diff.NewText);
    }

    [Fact]
    public void Rename_Preserves_Original_Path_For_Final_Diff()
    {
        var sourcePath = Path.Combine(_workspaceRoot, "src", "Old.cs");
        var destinationPath = Path.Combine(_workspaceRoot, "src", "New.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        File.WriteAllText(sourcePath, "class Old {}\n");

        FileStateTracker tracker = new("run_1", "session_1", _fileSnapshotService, _diffService);

        tracker.CaptureBaseline(sourcePath);
        var sourceSnapshot = _fileSnapshotService.CaptureFile(sourcePath);
        File.Move(sourcePath, destinationPath);
        tracker.RecordRename(sourcePath, destinationPath, sourceSnapshot);

        var diff = tracker.GetDiff("src/New.cs");
        var pending = tracker.DrainPendingChanges();

        Assert.NotNull(diff);
        Assert.Equal("renamed", diff.Status);
        Assert.Equal("src/Old.cs", diff.OldPath);
        Assert.Contains(pending, static change => change.Status == "renamed" && change.OldPath == "src/Old.cs" && change.Path == "src/New.cs");
    }

    [Fact]
    public void Added_Then_Deleted_File_Has_No_Final_Diff()
    {
        var filePath = Path.Combine(_workspaceRoot, "src", "Ephemeral.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        FileStateTracker tracker = new("run_1", "session_1", _fileSnapshotService, _diffService);

        tracker.CaptureBaseline(filePath);
        File.WriteAllText(filePath, "temporary\n");
        tracker.CaptureCurrent(filePath);
        File.Delete(filePath);
        tracker.RecordDelete(filePath);

        Assert.Empty(tracker.GetChanges());
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRoot))
        {
            Directory.Delete(_workspaceRoot, recursive: true);
        }
    }
}
