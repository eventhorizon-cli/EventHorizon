using System.ComponentModel;
using EventHorizon.Tools;

namespace EventHorizon.Workspace;

public interface IWorkspaceService
{
    string WorkspaceRoot { get; }

    [Description("High-level description of the configured workspace root and its top-level entries.")]
    string DescribeWorkspace();

    [Description("List files and folders inside a workspace directory.")]
    string ListDir([Description("The directory path to inspect. Use an empty string to list the workspace root.")] string path);

    [Description("Open an existing file and return line-numbered content.")]
    string OpenFile(
        [Description("The file path to open inside the workspace.")] string filePath,
        [Description("Whether to return a shorter preview instead of the standard view.")] bool isPreview = false);

    [Description("Read a file from the workspace and return line-numbered text.")]
    string ReadFileTool(
        [Description("The file path to read inside the workspace.")] string filePath,
        [Description("The 1-based starting line number to read from.")] int? offset = null,
        [Description("The maximum number of lines to return.")] int? limit = null);

    [Description("Create a new file inside the workspace.")]
    string CreateFile(
        [Description("The file path to create inside the workspace.")] string filePath,
        [Description("The full file content to write.")] string content);

    [Description("Replace one uniquely matched snippet in an existing file.")]
    string InsertEditIntoFile(
        [Description("The file path to edit inside the workspace.")] string filePath,
        [Description("The exact existing text to replace. It must match exactly one region.")] string searchText,
        [Description("The replacement text to insert.")] string replacementText);

    [Description("Apply a structured patch to a single file inside the workspace.")]
    string ApplyPatch(
        [Description("The file path the patch targets.")] string filePath,
        [Description("The patch content to apply.")] string input);

    [Description("Find files in the workspace by glob-style query.")]
    string FileSearch(
        [Description("The glob-style file query to match.")] string query,
        [Description("The maximum number of matching file paths to return.")] int maxResults = 200);

    [Description("Search matching lines in workspace files using plain text or regex.")]
    string GrepSearch(
        [Description("The plain text or regex pattern to search for.")] string query,
        [Description("Whether the query should be treated as a regular expression.")] bool isRegexp = false,
        [Description("An optional glob pattern to limit which files are searched.")] string includePattern = "*");

    [Description("Search the workspace semantically and return ranked snippets.")]
    string SemanticSearch(
        [Description("The semantic search query to evaluate.")] string query,
        [Description("The maximum number of ranked snippets to return.")] int maxResults = 8);

    [Description("Run a terminal command in the workspace or start it in the background.")]
    Task<string> RunInTerminalAsync(
        [Description("The terminal command to execute.")] string command,
        [Description("Whether the command should be started as a background session.")] bool isBackground);

    [Description("Get stdout, stderr, status, and exit code for a background terminal session.")]
    string GetTerminalOutput([Description("The id of the background terminal session.")] string id);

    [Description("Run workspace diagnostics and return matched errors for specific files.")]
    Task<string> GetErrorsAsync([Description("The file paths to collect diagnostics for.")] string[] filePaths);

    [Description("Validate package versions against OSV vulnerability data.")]
    Task<string> ValidateCvesAsync(
        [Description("The dependency coordinates to validate, such as package@version.")] string[] dependencies,
        [Description("The package ecosystem to validate against.")] string ecosystem);

    Task<string> RunShellAsync(string command, int timeoutSeconds, CancellationToken cancellationToken);
}
