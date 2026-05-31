using EventHorizon.Tools;
using EventHorizon.Workspace;
using EventHorizon.Workspace.Diff;

namespace EventHorizon.Tests.Tools;

public sealed class ToolCatalogTests : IDisposable
{
    private readonly string _workspaceRoot;
    private readonly WorkspaceContext _workspaceContext;

    public ToolCatalogTests()
    {
        _workspaceRoot = Path.Combine(Path.GetTempPath(), "eventhorizon-tool-catalog-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspaceRoot);
        _workspaceContext = new WorkspaceContext(_workspaceRoot);
    }

    [Fact]
    public void Create_Uses_SnakeCase_Tool_Names()
    {
        WorkspaceService workspaceService = new(
            _workspaceContext,
            new ShellCommandRunner(),
            new FileSnapshotService(_workspaceContext),
            new FileStateTrackerAccessor());
        var catalog = new ToolCatalog();
        var tools = catalog.Create(workspaceService);
        var names = tools.Select(static tool => tool.Name).ToArray();

        Assert.Equal(
            ["apply_patch", "ask_questions", "create_file", "file_search", "get_errors", "get_terminal_output", "grep_search", "insert_edit_into_file", "list_dir", "open_file", "read_file", "run_in_terminal", "run_subagent", "semantic_search", "validate_cves"],
            names);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRoot))
        {
            Directory.Delete(_workspaceRoot, recursive: true);
        }
    }
}
