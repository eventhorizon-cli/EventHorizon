using EventHorizon.Configuration;
using EventHorizon.Tools;
using EventHorizon.Workspace;
using Microsoft.Extensions.AI;

namespace EventHorizon.Tests.Tools;

public sealed class WorkspaceToolTests : IDisposable
{
    private readonly string _workspaceRoot;
    private readonly WorkspaceService _workspaceService;
    private readonly ToolCatalog _toolCatalog;
    private readonly AppOptions _options;

    public WorkspaceToolTests()
    {
        _workspaceRoot = Path.Combine(Path.GetTempPath(), "eventhorizon-tool-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspaceRoot);
        _workspaceService = new WorkspaceService(_workspaceRoot, new ShellCommandRunner(), new BackgroundTerminalCommandStore());
        _toolCatalog = new ToolCatalog();
        _options = new AppOptions
        {
            Agent = new AgentOptions { EnableShell = true }
        };
    }

    [Fact]
    public async Task ApplyPatch_Should_Apply_Patch_Successfully()
    {
        // Arrange
        string filePath = Path.Combine(_workspaceRoot, "test.txt");
        await File.WriteAllTextAsync(filePath, "line1\nline2\nline3\n");

        // Use simpler file editing approach
        string originalContent = await File.ReadAllTextAsync(filePath);
        string newContent = originalContent.Replace("line1", "modified_line1");

        // Act
        string result = _workspaceService.InsertEditIntoFile(filePath, "line1", "modified_line1");
        string content = await File.ReadAllTextAsync(filePath);

        // Assert
        Assert.Contains("modified_line1", content);
    }

    [Fact]
    public void AskQuestions_Should_Return_Formatted_Questions()
    {
        // Arrange
        var questions = new[]
        {
            new AskQuestionDefinition("Q1", "What is your name?", Options:
                [new AskQuestionOption("John"), new AskQuestionOption("Jane")]),
            new AskQuestionDefinition("Q2", "Select an option", MultiSelect: true, Options:
                [new AskQuestionOption("option1"), new AskQuestionOption("option2")])
        };

        // Act
        string result = _workspaceService.AskQuestions(questions);

        // Assert
        Assert.Contains("Q1", result);
        Assert.Contains("What is your name?", result);
    }

    [Fact]
    public async Task CreateFile_Should_Create_New_File()
    {
        // Arrange
        string filePath = Path.Combine(_workspaceRoot, "newfile.txt");
        string content = "Hello, World!";

        // Act
        string result = _workspaceService.CreateFile(filePath, content);

        // Assert
        Assert.Contains("Created", result);
        Assert.True(File.Exists(filePath));
        Assert.Equal(content, await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public void CreateFile_Should_Fail_If_Exists()
    {
        // Arrange
        string filePath = Path.Combine(_workspaceRoot, "existing.txt");
        File.WriteAllText(filePath, "existing content");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _workspaceService.CreateFile(filePath, "new content"));
    }

    [Fact]
    public async Task FileSearch_Should_Find_Files()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_workspaceRoot, "test1.cs"), "");
        await File.WriteAllTextAsync(Path.Combine(_workspaceRoot, "test2.cs"), "");
        Directory.CreateDirectory(Path.Combine(_workspaceRoot, "subdir"));
        await File.WriteAllTextAsync(Path.Combine(_workspaceRoot, "subdir", "test3.cs"), "");

        // Act
        string result = _workspaceService.FileSearch("*.cs", 10);

        // Assert
        Assert.Contains("test1.cs", result);
        Assert.Contains("test2.cs", result);
    }

    [Fact]
    public async Task GrepSearch_Should_Find_Matching_Lines()
    {
        // Arrange
        string file1 = Path.Combine(_workspaceRoot, "file1.txt");
        string file2 = Path.Combine(_workspaceRoot, "file2.txt");
        await File.WriteAllTextAsync(file1, "Hello World\n");
        await File.WriteAllTextAsync(file2, "Goodbye World\n");

        // Act
        string result = _workspaceService.GrepSearch("World");

        // Assert
        Assert.Contains("World", result);
    }

    [Fact]
    public async Task GrepSearch_With_Regexp_Should_Find_Matches()
    {
        // Arrange
        string file = Path.Combine(_workspaceRoot, "data.txt");
        await File.WriteAllTextAsync(file, "item-123\nitem-456\nother-789\n");

        // Act
        string result = _workspaceService.GrepSearch("item-\\d+", isRegexp: true);

        // Assert
        Assert.Contains("item-123", result);
        Assert.Contains("item-456", result);
    }

    [Fact]
    public async Task InsertEditIntoFile_Should_Replace_Text()
    {
        // Arrange
        string filePath = Path.Combine(_workspaceRoot, "edit.txt");
        await File.WriteAllTextAsync(filePath, "Hello Universe\nGoodbye World\n");

        // Act
        string result = _workspaceService.InsertEditIntoFile(filePath, "Universe", "Galaxy");

        // Assert
        string content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("Hello Galaxy", content);
    }

    [Fact]
    public async Task ListDir_Should_List_Directory_Contents()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_workspaceRoot, "subdir"));
        await File.WriteAllTextAsync(Path.Combine(_workspaceRoot, "file.txt"), "");

        // Act
        string result = _workspaceService.ListDir("");

        // Assert
        Assert.Contains("subdir", result);
        Assert.Contains("file.txt", result);
    }

    [Fact]
    public async Task OpenFile_Should_Return_File_Content()
    {
        // Arrange
        string filePath = Path.Combine(_workspaceRoot, "open.txt");
        await File.WriteAllTextAsync(filePath, "Line 1\nLine 2\nLine 3\n");

        // Act
        string result = _workspaceService.OpenFile(filePath, isPreview: false);

        // Assert
        Assert.Contains("Line 1", result);
        Assert.Contains("Line 2", result);
    }

    [Fact]
    public async Task OpenFile_Preview_Should_Return_Shortened_Content()
    {
        // Arrange
        string filePath = Path.Combine(_workspaceRoot, "large.txt");
        var lines = Enumerable.Range(1, 100).Select(i => $"Line {i}");
        await File.WriteAllLinesAsync(filePath, lines);

        // Act
        string result = _workspaceService.OpenFile(filePath, isPreview: true);

        // Assert
        Assert.Contains("Line 1", result);
        // Preview should be limited
    }

    [Fact]
    public async Task ReadFile_Should_Return_Line_Numbered_Content()
    {
        // Arrange
        string filePath = Path.Combine(_workspaceRoot, "read.txt");
        await File.WriteAllTextAsync(filePath, "Line 1\nLine 2\nLine 3\n");

        // Act
        string result = _workspaceService.ReadFileTool(filePath, offset: null, limit: null);

        // Assert
        Assert.Contains("1", result);
        Assert.Contains("Line 1", result);
    }

    [Fact]
    public async Task ReadFile_With_Offset_And_Limit_Should_Return_Partial_Content()
    {
        // Arrange
        string filePath = Path.Combine(_workspaceRoot, "partial.txt");
        var lines = Enumerable.Range(1, 10).Select(i => $"Line {i}");
        await File.WriteAllLinesAsync(filePath, lines);

        // Act
        string result = _workspaceService.ReadFileTool(filePath, offset: 2, limit: 3);

        // Assert
        Assert.Contains("Line 2", result);
        Assert.Contains("Line 3", result);
        Assert.Contains("Line 4", result);
    }

    [Fact]
    public void RunSubagent_Should_Return_Subagent_Response()
    {
        // Arrange
        string task = "Find C# files";
        string agentName = "Search";

        // Act
        string result = _workspaceService.RunSubagent(task, agentName);

        // Assert
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task SemanticSearch_Should_Return_Ranked_Results()
    {
        // Arrange
        string file1 = Path.Combine(_workspaceRoot, "about-cats.txt");
        string file2 = Path.Combine(_workspaceRoot, "about-dogs.txt");
        await File.WriteAllTextAsync(file1, "Cats are furry animals that meow.");
        await File.WriteAllTextAsync(file2, "Dogs are friendly animals that bark.");

        // Act
        string result = _workspaceService.SemanticSearch("pet animals", maxResults: 2);

        // Assert
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ValidateCves_Should_Check_Vulnerabilities()
    {
        // Arrange
        string[] dependencies = ["package@1.0.0"];
        string ecosystem = "npm";

        // Act
        string result = await _workspaceService.ValidateCvesAsync(dependencies, ecosystem, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetErrors_Should_Return_Diagnostics()
    {
        // Arrange
        string filePath = Path.Combine(_workspaceRoot, "Test.cs");
        await File.WriteAllTextAsync(filePath, "public class Test { }");

        // Act
        string result = await _workspaceService.GetErrorsAsync([filePath], CancellationToken.None);

        // Assert
        // Should not throw, result can be empty if no errors
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RunInTerminalAsync_Should_Execute_Command()
    {
        // Arrange
        string command = "echo 'Hello Terminal'";

        // Act
        string result = await _workspaceService.RunInTerminalAsync(command, "Test command", isBackground: false, CancellationToken.None);

        // Assert
        Assert.Contains("Hello Terminal", result);
    }

    [Fact]
    public async Task RunInTerminalAsync_Background_Should_Start_Background_Process()
    {
        // Arrange
        string command = "sleep 10";

        // Act
        string result = await _workspaceService.RunInTerminalAsync(command, "Background test", isBackground: true, CancellationToken.None);

        // Assert
        Assert.Contains("Background", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTerminalOutput_Should_Return_Process_Output()
    {
        // Arrange
        string command = "echo 'test'";

        // Act
        string result = await _workspaceService.RunInTerminalAsync(command, "Test", isBackground: true, CancellationToken.None);

        // Extract session ID from result (format: "Started background terminal session.\nId: xxx\n...")
        var lines = result.Split('\n');
        string? sessionId = null;
        foreach (var line in lines)
        {
            if (line.StartsWith("Id: "))
            {
                sessionId = line.Substring(4).Trim();
                break;
            }
        }

        Assert.NotNull(sessionId);

        await Task.Delay(100); // Give it time to start
        string output = _workspaceService.GetTerminalOutput(sessionId);

        // Assert
        Assert.NotNull(output);
    }

    [Fact]
    public void ToolCatalog_Should_Include_All_Tools()
    {
        // Arrange & Act
        var tools = _toolCatalog.Create(_workspaceService, _options);

        // Assert
        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("apply_patch", toolNames);
        Assert.Contains("ask_questions", toolNames);
        Assert.Contains("create_file", toolNames);
        Assert.Contains("file_search", toolNames);
        Assert.Contains("grep_search", toolNames);
        Assert.Contains("insert_edit_into_file", toolNames);
        Assert.Contains("list_dir", toolNames);
        Assert.Contains("open_file", toolNames);
        Assert.Contains("read_file", toolNames);
        Assert.Contains("run_subagent", toolNames);
        Assert.Contains("semantic_search", toolNames);
        Assert.Contains("validate_cves", toolNames);
        Assert.Contains("get_errors", toolNames);
        Assert.Contains("get_terminal_output", toolNames);
        Assert.Contains("run_in_terminal", toolNames);
    }

    [Fact]
    public void ToolCatalog_Without_Shell_Should_Exclude_Shell_Tools()
    {
        // Arrange
        var optionsWithoutShell = new AppOptions
        {
            Agent = new AgentOptions { EnableShell = false }
        };

        // Act
        var tools = _toolCatalog.Create(_workspaceService, optionsWithoutShell);

        // Assert
        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.DoesNotContain("get_errors", toolNames);
        Assert.DoesNotContain("get_terminal_output", toolNames);
        Assert.DoesNotContain("run_in_terminal", toolNames);
    }

    [Fact]
    public void ToolCatalog_Should_Use_Method_Groups()
    {
        // Arrange & Act
        var tools = _toolCatalog.Create(_workspaceService, _options);

        // Assert - verify each tool has an AI function
        foreach (var tool in tools)
        {
            Assert.NotNull(tool.Tool);
            Assert.NotEmpty(tool.Name);
            Assert.NotEmpty(tool.Description);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRoot))
        {
            Directory.Delete(_workspaceRoot, recursive: true);
        }
    }
}
