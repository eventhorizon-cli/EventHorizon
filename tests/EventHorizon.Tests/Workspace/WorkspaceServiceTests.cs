using EventHorizon.Tools;
using EventHorizon.Workspace;

namespace EventHorizon.Tests.Workspace;

public sealed class WorkspaceServiceTests : IDisposable
{
    private readonly string _root;

    public WorkspaceServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "eventhorizon-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void Write_And_Read_File_Uses_Workspace_Relative_Paths()
    {
        WorkspaceService service = CreateService();

        service.WriteFile("src/demo.txt", "line1\nline2");
        string content = service.ReadFile("src/demo.txt", startLine: 2, maxLines: 1);

        Assert.Equal("2: line2", content);
    }

    [Fact]
    public void ResolvePath_Blocks_Path_Escape()
    {
        WorkspaceService service = CreateService();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => service.ReadFile("../outside.txt"));

        Assert.Contains("escapes", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FindFiles_And_Grep_Return_Matching_Content()
    {
        WorkspaceService service = CreateService();
        service.WriteFile("src/app.cs", "Console.WriteLine(\"Hello\");");
        service.WriteFile("README.md", "Hello workspace");

        string files = service.FindFiles("*.md");
        string grep = service.Grep("Hello");

        Assert.Contains("README.md", files);
        Assert.Contains("README.md:1: Hello workspace", grep);
    }

    [Fact]
    public void CreateFile_And_InsertEditIntoFile_Update_Content()
    {
        WorkspaceService service = CreateService();

        string createResult = service.CreateFile("src/editor.cs", "class Demo\n{\n    void Run() { }\n}\n");
        string editResult = service.InsertEditIntoFile("src/editor.cs", "void Run() { }", "void Run()\n    {\n        Console.WriteLine(\"updated\");\n    }");
        string content = service.ReadFile("src/editor.cs");

        Assert.Contains("Created src/editor.cs", createResult);
        Assert.Contains("Updated 1 region", editResult);
        Assert.Contains("Console.WriteLine(\"updated\")", content);
    }

    [Fact]
    public void ApplyPatch_Updates_Multiple_Regions()
    {
        WorkspaceService service = CreateService();
        service.WriteFile("src/patch-demo.txt", "alpha\nbeta\ngamma\n");

        string patch = """
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

        string result = service.ApplyPatch("src/patch-demo.txt", patch, "update sample lines");
        string content = File.ReadAllText(Path.Combine(_root, "src", "patch-demo.txt"));

        Assert.Contains("Applied patch", result);
        Assert.Equal("alpha\nbeta-2\ngamma\ndelta\n", content);
    }

    [Fact]
    public void SemanticSearch_Returns_Relevant_Snippet()
    {
        WorkspaceService service = CreateService();
        service.WriteFile("src/search.cs", "public sealed class SearchDemo\n{\n    public string RenderExplorerPanel() => \"explorer\";\n}\n");

        string result = service.SemanticSearch("render explorer panel", 5);

        Assert.Contains("src/search.cs", result);
        Assert.Contains("RenderExplorerPanel", result);
    }

    [Fact]
    public void AskQuestions_Formats_Structured_Output()
    {
        WorkspaceService service = CreateService();

        string result = service.AskQuestions(
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
        WorkspaceService service = CreateService();

        string start = await service.RunInTerminalAsync("dotnet --info | cat", "collect environment", isBackground: true, CancellationToken.None);
        string id = start.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Single(line => line.StartsWith("Id: ", StringComparison.Ordinal))[4..];

        var output = string.Empty;
        for (int attempt = 0; attempt < 20; attempt++)
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

    private WorkspaceService CreateService() => new(_root, new ShellCommandRunner());

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}


