using EventHorizon.Configuration;
using EventHorizon.Workspace;
using Microsoft.Extensions.AI;

namespace EventHorizon.Tools;

public sealed class ToolCatalog : IToolCatalogFactory
{
    public IReadOnlyList<ToolDescriptor> Create(WorkspaceService workspaceService, AppOptions options)
    {
        List<ToolDescriptor> tools =
        [
            new(
                "apply_patch",
                "Apply a structured patch to a single file. Use this for multi-region edits that include exact context lines.",
                IsReadOnly: false,
                IsConcurrencySafe: false,
                AIFunctionFactory.Create(workspaceService.ApplyPatch, name: "apply_patch", description: "Apply a structured patch to a single file inside the workspace.")),
            new(
                "ask_questions",
                "Prepare a structured set of clarifying questions for the user when more input is needed.",
                IsReadOnly: true,
                IsConcurrencySafe: true,
                AIFunctionFactory.Create(workspaceService.AskQuestions, name: "ask_questions", description: "Format one or more structured clarifying questions for the user.")),
            new(
                "create_file",
                "Create a new file inside the workspace. This fails if the target file already exists.",
                IsReadOnly: false,
                IsConcurrencySafe: false,
                AIFunctionFactory.Create(workspaceService.CreateFile, name: "create_file", description: "Create a new file inside the workspace.")),
            new(
                "file_search",
                "Find files by glob-style path query. Prefer this when you know the filename shape but not the exact location.",
                IsReadOnly: true,
                IsConcurrencySafe: true,
                AIFunctionFactory.Create(workspaceService.FileSearch, name: "file_search", description: "Find files in the workspace by glob-style query.")),
            new(
                "grep_search",
                "Search file contents using plain text or regex matching with an optional include glob.",
                IsReadOnly: true,
                IsConcurrencySafe: true,
                AIFunctionFactory.Create(workspaceService.GrepSearch, name: "grep_search", description: "Search matching lines in workspace files using plain text or regex.")),
            new(
                "insert_edit_into_file",
                "Edit a single existing file by replacing one uniquely matched snippet with new text.",
                IsReadOnly: false,
                IsConcurrencySafe: false,
                AIFunctionFactory.Create(workspaceService.InsertEditIntoFile, name: "insert_edit_into_file", description: "Replace one uniquely matched snippet in an existing file.")),
            new(
                "list_dir",
                "List files and folders for a directory inside the workspace.",
                IsReadOnly: true,
                IsConcurrencySafe: true,
                AIFunctionFactory.Create(workspaceService.ListDir, name: "list_dir", description: "List files and folders inside a workspace directory.")),
            new(
                "open_file",
                "Open an existing file for context review. Preview mode returns a shorter view.",
                IsReadOnly: true,
                IsConcurrencySafe: true,
                AIFunctionFactory.Create(workspaceService.OpenFile, name: "open_file", description: "Open an existing file and return line-numbered content.")),
            new(
                "read_file",
                "Read line-numbered file content. Use targeted ranges when files are large.",
                IsReadOnly: true,
                IsConcurrencySafe: true,
                AIFunctionFactory.Create(workspaceService.ReadFileTool, name: "read_file", description: "Read a file from the workspace and return line-numbered text.")),
            new(
                "run_subagent",
                "Launch a specialized helper for delegated tasks. The built-in Search agent returns autonomous workspace findings.",
                IsReadOnly: true,
                IsConcurrencySafe: true,
                AIFunctionFactory.Create(workspaceService.RunSubagent, name: "run_subagent", description: "Run the built-in Search subagent and return its findings.")),
            new(
                "semantic_search",
                "Search the workspace by meaning using keyword-based snippet ranking.",
                IsReadOnly: true,
                IsConcurrencySafe: true,
                AIFunctionFactory.Create(workspaceService.SemanticSearch, name: "semantic_search", description: "Search the workspace semantically and return ranked snippets.")),
            new(
                "validate_cves",
                "Check package versions against OSV vulnerability data for a supported ecosystem.",
                IsReadOnly: true,
                IsConcurrencySafe: true,
                AIFunctionFactory.Create(workspaceService.ValidateCvesAsync, name: "validate_cves", description: "Validate package versions against OSV vulnerability data.")),
        ];

        tools.AddRange(
        [
            new ToolDescriptor(
                "get_errors",
                "Run workspace diagnostics and return compile or analyzer output for specific files.",
                IsReadOnly: true,
                IsConcurrencySafe: false,
                AIFunctionFactory.Create(
                    workspaceService.GetErrorsAsync,
                    name: "get_errors",
                    description: "Run workspace diagnostics and return matched errors for specific files.")),
            new ToolDescriptor(
                "get_terminal_output",
                "Inspect the current output for a previously started background terminal session.",
                IsReadOnly: true,
                IsConcurrencySafe: false,
                AIFunctionFactory.Create(
                    workspaceService.GetTerminalOutput,
                    name: "get_terminal_output",
                    description: "Get stdout, stderr, status, and exit code for a background terminal session.")),
            new ToolDescriptor(
                "run_in_terminal",
                "Run a terminal command in the workspace. Use background mode for long-running tasks.",
                IsReadOnly: false,
                IsConcurrencySafe: false,
                AIFunctionFactory.Create(
                    workspaceService.RunInTerminalAsync,
                    name: "run_in_terminal",
                    description: "Run a terminal command in the workspace or start it in the background.")),
        ]);

        return tools.OrderBy(static tool => tool.Name, StringComparer.Ordinal).ToArray();
    }
}
