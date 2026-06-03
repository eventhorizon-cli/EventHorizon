using System.Runtime.InteropServices;
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
        var systemEnvironment = DescribeSystemEnvironment();
        var shellTooling = DescribeShellTooling();

        return new SessionContextSnapshot(
            CurrentDate: $"Today's date is {DateTimeOffset.Now:yyyy-MM-dd}.",
            WorkspaceRoot: workspaceRoot,
            WorkspaceSummary: workspaceSummary,
            GitStatus: gitStatus,
            ProjectInstructions: projectInstructions,
            SystemEnvironment: systemEnvironment,
            ShellTooling: shellTooling);
    }

    private async Task<string> TryGetGitStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var status = await _workspaceService.RunShellAsync("git --no-pager status --short --branch | cat", 15, cancellationToken).ConfigureAwait(false);
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

    private static string DescribeSystemEnvironment()
    {
        var operatingSystem = GetOperatingSystemName();
        var osDescription = RuntimeInformation.OSDescription.Trim();
        var architecture = RuntimeInformation.OSArchitecture.ToString();
        var shellPath = ShellCommandRunner.GetDefaultShellPath();
        var shellName = Path.GetFileName(shellPath);

        return string.Join(
            Environment.NewLine,
            [
                $"Operating system: {operatingSystem} ({osDescription})",
                $"OS architecture: {architecture}",
                $"Default shell: {shellName} ({shellPath})",
            ]);
    }

    private static string DescribeShellTooling()
    {
        var shellPath = ShellCommandRunner.GetDefaultShellPath();
        var invocation = ShellCommandRunner.GetInvocationExample();

        return string.Join(
            Environment.NewLine,
            [
                "- `run_in_terminal` executes a shell command from the workspace root.",
                "- Use `run_in_terminal` for builds, tests, git inspection, and short-lived scripts.",
                "- Set `isBackground=true` for long-running processes such as local servers or watch tasks.",
                "- Foreground terminal commands time out after 120 seconds.",
                "- Use `get_terminal_output` with the returned session id to inspect stdout, stderr, exit code, and status for background commands.",
                $"- Current shell invocation pattern: `{invocation}`.",
                $"- Current shell executable: `{shellPath}`.",
            ]);
    }

    private static string GetOperatingSystemName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "Windows";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "macOS";
        }

        if (OperatingSystem.IsLinux())
        {
            return "Linux";
        }

        return "Unknown";
    }
}

