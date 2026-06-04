using System.Diagnostics;
using System.Text;
using EventHorizon.Workspace;

namespace EventHorizon.Engine.Sessions;

public interface ISessionContextBuilder
{
    Task<SessionContextSnapshot> BuildAsync(CancellationToken cancellationToken);
}

public sealed class SessionContextBuilder : ISessionContextBuilder
{
    private readonly IWorkspaceService _workspaceService;

    public SessionContextBuilder(IWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    public async Task<SessionContextSnapshot> BuildAsync(CancellationToken cancellationToken)
    {
        var workspaceRoot = _workspaceService.WorkspaceRoot;
        var workspaceSummary = _workspaceService.DescribeWorkspace();
        var gitStatus = await TryGetGitStatusAsync(cancellationToken).ConfigureAwait(false);
        var projectInstructions = ReadProjectInstructions(workspaceRoot);

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
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--no-pager status --short --branch",
                    WorkingDirectory = _workspaceService.WorkspaceRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            process.Start();

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(15));

            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutSource.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeoutSource.Token);
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);

            var stdout = (await stdoutTask.ConfigureAwait(false)).TrimEnd();
            var stderr = (await stderrTask.ConfigureAwait(false)).TrimEnd();
            var status = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
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
        foreach (var candidate in new[]
                 {
                     Path.Combine(workspaceRoot, "AGENTS.md"),
                     Path.Combine(workspaceRoot, "README.md"),
                 })
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            var content = File.ReadAllText(candidate);
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
            ? "No AGENTS.md, or README.md guidance file was found at the workspace root."
            : builder.ToString().TrimEnd();
    }

}

