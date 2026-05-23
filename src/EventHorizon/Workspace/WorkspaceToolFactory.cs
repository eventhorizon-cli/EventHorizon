using EventHorizon.Tools;
using Microsoft.Extensions.AI;

namespace EventHorizon.Workspace;

public static class WorkspaceToolFactory
{
    public static IReadOnlyList<AITool> CreateTools(WorkspaceService workspaceService, Configuration.AppOptions options)
    {
        List<AITool> tools =
        [
            AIFunctionFactory.Create((string filePath, string input, string explanation) => workspaceService.ApplyPatch(filePath, input, explanation), name: "apply_patch", description: "Apply a structured patch to a single workspace file."),
            AIFunctionFactory.Create((AskQuestionDefinition[] questions) => workspaceService.AskQuestions(questions), name: "ask_questions", description: "Format one or more clarifying questions for the user."),
            AIFunctionFactory.Create((string filePath, string content) => workspaceService.CreateFile(filePath, content), name: "create_file", description: "Create a new file inside the workspace."),
            AIFunctionFactory.Create((string query, int maxResults) => workspaceService.FileSearch(query, maxResults), name: "file_search", description: "Find files in the workspace by glob-style query."),
            AIFunctionFactory.Create((string query, bool isRegexp, string includePattern) => workspaceService.GrepSearch(query, isRegexp, includePattern), name: "grep_search", description: "Search matching lines in workspace files using plain text or regex."),
            AIFunctionFactory.Create((string filePath, string searchText, string replacementText) => workspaceService.InsertEditIntoFile(filePath, searchText, replacementText), name: "insert_edit_into_file", description: "Replace one uniquely matched snippet in an existing file."),
            AIFunctionFactory.Create((string path) => workspaceService.ListDir(path), name: "list_dir", description: "List files and folders inside a workspace directory."),
            AIFunctionFactory.Create((string filePath, bool isPreview) => workspaceService.OpenFile(filePath, isPreview), name: "open_file", description: "Open an existing file and return line-numbered content."),
            AIFunctionFactory.Create((string filePath, int? offset, int? limit) => workspaceService.ReadFileTool(filePath, offset, limit), name: "read_file", description: "Read a file from the workspace and return line-numbered text."),
            AIFunctionFactory.Create((string task, string agentName, string? description) => workspaceService.RunSubagent(task, agentName, description), name: "run_subagent", description: "Run the built-in Search subagent and return its findings."),
            AIFunctionFactory.Create((string query, int maxResults) => workspaceService.SemanticSearch(query, maxResults), name: "semantic_search", description: "Search the workspace semantically and return ranked snippets."),
            AIFunctionFactory.Create((string[] dependencies, string ecosystem, CancellationToken cancellationToken) => workspaceService.ValidateCvesAsync(dependencies, ecosystem, cancellationToken), name: "validate_cves", description: "Validate package versions against OSV vulnerability data.")
        ];

        if (options.Agent.EnableShell)
        {
            tools.AddRange(
            [
                AIFunctionFactory.Create((string[] filePaths, CancellationToken cancellationToken) => workspaceService.GetErrorsAsync(filePaths, cancellationToken), name: "get_errors", description: "Run workspace diagnostics and return matched errors for specific files."),
                AIFunctionFactory.Create((string id) => workspaceService.GetTerminalOutput(id), name: "get_terminal_output", description: "Get stdout, stderr, status, and exit code for a background terminal session."),
                AIFunctionFactory.Create((string command, string explanation, bool isBackground, CancellationToken cancellationToken) => workspaceService.RunInTerminalAsync(command, explanation, isBackground, cancellationToken), name: "run_in_terminal", description: "Run a terminal command in the workspace or start it in the background.")
            ]);
        }

        return tools;
    }
}

