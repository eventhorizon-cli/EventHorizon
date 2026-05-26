using EventHorizon.Configuration;
using EventHorizon.Tools;
using EventHorizon.Workspace;

namespace EventHorizon.Tests.Tools;

public sealed class ToolCatalogTests : IDisposable
{
    private readonly string _workspaceRoot;

    public ToolCatalogTests()
    {
        _workspaceRoot = Path.Combine(Path.GetTempPath(), "eventhorizon-tool-catalog-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspaceRoot);
    }

    [Fact]
    public void Create_Uses_SnakeCase_Tool_Names()
    {
        WorkspaceService workspaceService = new(_workspaceRoot, new ShellCommandRunner());
        AppOptions options = new()
        {
            Agent = new AgentOptions
            {
                EnableShell = true,
            },
        };

        var catalog = new ToolCatalog();
        var tools = catalog.Create(workspaceService, options);
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

