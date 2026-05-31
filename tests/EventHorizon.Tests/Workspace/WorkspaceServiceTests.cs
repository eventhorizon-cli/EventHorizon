using EventHorizon.Tools;
using EventHorizon.Workspace;
using EventHorizon.Workspace.Diff;

namespace EventHorizon.Tests.Workspace;

public sealed class WorkspaceServiceTests : IDisposable
{
    private readonly string _root;
    private readonly WorkspaceContext _workspaceContext;

    public WorkspaceServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "eventhorizon-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _workspaceContext = new WorkspaceContext(_root);
    }

    [Fact]
    public void Write_And_Read_File_Uses_Workspace_Relative_Paths()
    {
        var service = CreateService();

        service.WriteFile("src/demo.txt", "line1\nline2");
        var content = service.ReadFile("src/demo.txt", startLine: 2, maxLines: 1);

        Assert.Equal("2: line2", content);
    }

    [Fact]
    public void ResolvePath_Blocks_Path_Escape()
    {
        var service = CreateService();

        var ex = Assert.Throws<InvalidOperationException>(() => service.ReadFile("../outside.txt"));

        Assert.Contains("escapes", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FindFiles_And_Grep_Return_Matching_Content()
    {
        var service = CreateService();
        service.WriteFile("src/app.cs", "Console.WriteLine(\"Hello\");");
        service.WriteFile("README.md", "Hello workspace");

        var files = service.FindFiles("*.md");
        var grep = service.Grep("Hello");

        Assert.Contains("README.md", files);
        Assert.Contains("README.md:1: Hello workspace", grep);
    }

    [Fact]
    public void CreateFile_And_InsertEditIntoFile_Update_Content()
    {
        var service = CreateService();

        var createResult = service.CreateFile("src/editor.cs", "class Demo\n{\n    void Run() { }\n}\n");
        var editResult = service.InsertEditIntoFile("src/editor.cs", "void Run() { }", "void Run()\n    {\n        Console.WriteLine(\"updated\");\n    }");
        var content = service.ReadFile("src/editor.cs");

        Assert.Contains("Created src/editor.cs", createResult);
        Assert.Contains("Updated 1 region", editResult);
        Assert.Contains("Console.WriteLine(\"updated\")", content);
    }

    [Fact]
    public void ApplyPatch_Updates_Multiple_Regions()
    {
        var service = CreateService();
        service.WriteFile("src/patch-demo.txt", "alpha\nbeta\ngamma\n");

        var patch = """
*** Begin Patch
*** Update File: src/patch-demo.txt
@@
 alpha
-beta
+beta-2
 gamma
@@
 beta-2
 gamma
+delta
*** End Patch
""";

        var result = service.ApplyPatch("src/patch-demo.txt", patch, "update sample lines");
        var content = File.ReadAllText(Path.Combine(_root, "src", "patch-demo.txt"));

        Assert.Contains("Applied patch", result);
        Assert.Equal("alpha\nbeta-2\ngamma\ndelta\n", content);
    }

    [Fact]
    public void SemanticSearch_Returns_Relevant_Snippet()
    {
        var service = CreateService();
        service.WriteFile("src/search.cs", "public sealed class SearchDemo\n{\n    public string RenderExplorerPanel() => \"explorer\";\n}\n");

        var result = service.SemanticSearch("render explorer panel", 5);

        Assert.Contains("src/search.cs", result);
        Assert.Contains("RenderExplorerPanel", result);
    }

    [Fact]
    public void AskQuestions_Formats_Structured_Output()
    {
        var service = CreateService();

        var result = service.AskQuestions(
        [
            new AskQuestionDefinition(
                "scope",
                "Which panel should be focused?",
                Options:
                [
                    new AskQuestionOption("Explorer"),
                    new AskQuestionOption("Inspector"),
                ])
        ]);

        Assert.Contains("scope", result);
        Assert.Contains("Explorer", result);
    }

    [Fact]
    public async Task RunInTerminal_Background_Session_Can_Be_Inspected()
    {
        var service = CreateService();

        var start = await service.RunInTerminalAsync("dotnet --info | cat", "collect environment", isBackground: true, CancellationToken.None);
        var id = start.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Single(line => line.StartsWith("Id: ", StringComparison.Ordinal))[4..];

        var output = string.Empty;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            output = service.GetTerminalOutput(id);
            if (output.Contains("Status: completed", StringComparison.Ordinal))
            {
                break;
            }

            await Task.Delay(100);
        }

        Assert.Contains("Id: ", output);
        Assert.Contains("Command: dotnet --info | cat", output);
    }

    private WorkspaceService CreateService()
        => new(
            _workspaceContext,
            new ShellCommandRunner(),
            new FileSnapshotService(_workspaceContext),
            new FileStateTrackerAccessor());

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
