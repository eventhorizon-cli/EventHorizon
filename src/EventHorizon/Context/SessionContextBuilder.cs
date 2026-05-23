using System.Text;
using EventHorizon.Workspace;

namespace EventHorizon.Context;

public interface ISessionContextBuilder
{
    Task<SessionContextSnapshot> BuildAsync(CancellationToken cancellationToken);
}

public sealed class SessionContextBuilder : ISessionContextBuilder
{
    private readonly WorkspaceService _workspaceService;

    public SessionContextBuilder(WorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    public async Task<SessionContextSnapshot> BuildAsync(CancellationToken cancellationToken)
    {
        var workspaceRoot = _workspaceService.WorkspaceRoot;
        string workspaceSummary = _workspaceService.DescribeWorkspace();
        string gitStatus = await TryGetGitStatusAsync(cancellationToken).ConfigureAwait(false);
        string projectInstructions = ReadProjectInstructions(workspaceRoot);

        return new SessionContextSnapshot(
            CurrentDate: $"Today's date is {DateTimeOffset.Now:yyyy-MM-dd}.",
            WorkspaceRoot: workspaceRoot,
            WorkspaceSummary: workspaceSummary,
            GitStatus: gitStatus,
            ProjectInstructions: projectInstructions);
    }

    private async Task<string> TryGetGitStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            string status = await _workspaceService.RunShellAsync("git --no-pager status --short --branch | cat", 15, cancellationToken).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(status)
                ? "Git status unavailable."
                : status;
        }
        catch (Exception ex)
        {
            return $"Git status unavailable: {ex.Message}";
        }
    }

    private static string ReadProjectInstructions(string workspaceRoot)
    {
        StringBuilder builder = new();
        foreach (string candidate in new[]
                 {
                     Path.Combine(workspaceRoot, "EVENTHORIZON.md"),
                     Path.Combine(workspaceRoot, "AGENTS.md"),
                     Path.Combine(workspaceRoot, "README.md"),
                 })
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            string content = File.ReadAllText(candidate);
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine().AppendLine();
            }

            builder.AppendLine($"[{Path.GetFileName(candidate)}]");
            builder.AppendLine(content.Length <= 4000 ? content : content[..4000]);
        }

        return builder.Length == 0
            ? "No EVENTHORIZON.md, AGENTS.md, or README.md guidance file was found at the workspace root."
            : builder.ToString().TrimEnd();
    }
}


