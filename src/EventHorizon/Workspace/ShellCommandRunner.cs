using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace EventHorizon.Workspace;

public sealed class ShellCommandRunner
{
    public async Task<ShellCommandResult> RunAsync(string command, string workingDirectory, int timeoutSeconds, CancellationToken cancellationToken)
    {
        (var fileName, var arguments) = GetShellInvocation(command);

        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        StringBuilder stdout = new();
        StringBuilder stderr = new();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
            }

            return new ShellCommandResult(-1, stdout.ToString().TrimEnd(), stderr.ToString().TrimEnd(), true);
        }

        return new ShellCommandResult(process.ExitCode, stdout.ToString().TrimEnd(), stderr.ToString().TrimEnd(), false);
    }

    internal static (string FileName, string Arguments) GetShellInvocation(string command)
    {
        if (OperatingSystem.IsWindows())
        {
            return ("cmd.exe", "/c " + command);
        }

        var shell = Environment.GetEnvironmentVariable("SHELL");
        if (string.IsNullOrWhiteSpace(shell) || !File.Exists(shell))
        {
            shell = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "/bin/zsh" : "/bin/bash";
        }

        return (shell, "-lc \"" + command.Replace("\"", "\\\"") + "\"");
    }
}

public readonly record struct ShellCommandResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut)
{
    public override string ToString() => $"ExitCode: {ExitCode}\nTimedOut: {TimedOut}\nStdout:\n{StandardOutput}\n\nStderr:\n{StandardError}";
}

