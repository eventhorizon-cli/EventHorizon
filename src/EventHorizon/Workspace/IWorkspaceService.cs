using EventHorizon.Tools;

namespace EventHorizon.Workspace;

public interface IWorkspaceService
{
    string WorkspaceRoot { get; }

    string DescribeWorkspace();

    string ListDir(string path);

    string OpenFile(string filePath, bool isPreview = false);

    string ReadFileTool(string filePath, int? offset = null, int? limit = null);

    string CreateFile(string filePath, string content);

    string InsertEditIntoFile(string filePath, string searchText, string replacementText);

    string ApplyPatch(string filePath, string input, string explanation);

    string FileSearch(string query, int maxResults = 200);

    string GrepSearch(string query, bool isRegexp = false, string includePattern = "*");

    string SemanticSearch(string query, int maxResults = 8);

    Task<string> RunInTerminalAsync(string command, string explanation, bool isBackground, CancellationToken cancellationToken);

    string GetTerminalOutput(string id);

    Task<string> GetErrorsAsync(string[] filePaths, CancellationToken cancellationToken);

    Task<string> ValidateCvesAsync(string[] dependencies, string ecosystem, CancellationToken cancellationToken);

    string AskQuestions(AskQuestionDefinition[] questions);

    string RunSubagent(string task, string agentName, string? description = null);

    Task<string> RunShellAsync(string command, int timeoutSeconds, CancellationToken cancellationToken);
}
