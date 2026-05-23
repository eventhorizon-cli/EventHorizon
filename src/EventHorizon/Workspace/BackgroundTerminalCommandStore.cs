using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace EventHorizon.Workspace;

public sealed class BackgroundTerminalCommandStore
{
    private readonly ConcurrentDictionary<string, BackgroundTerminalCommand> _commands = new(StringComparer.Ordinal);

    public string Start(string command, string workingDirectory)
    {
        string id = Guid.NewGuid().ToString("N");
        BackgroundTerminalCommand session = BackgroundTerminalCommand.Start(id, command, workingDirectory);
        _commands[id] = session;
        return id;
    }

    public string GetOutput(string id)
    {
        if (!_commands.TryGetValue(id, out BackgroundTerminalCommand? command))
        {
            throw new InvalidOperationException($"The terminal session '{id}' was not found.");
        }

        return command.FormatOutput();
    }

    private sealed class BackgroundTerminalCommand
    {
        private readonly Process _process;
        private readonly StringBuilder _stdout = new();
        private readonly StringBuilder _stderr = new();
        private readonly object _sync = new();

        private BackgroundTerminalCommand(string id, string command, Process process)
        {
            Id = id;
            Command = command;
            _process = process;
            StartedAtUtc = DateTimeOffset.UtcNow;
        }

        public string Id { get; }

        public string Command { get; }

        public DateTimeOffset StartedAtUtc { get; }

        public static BackgroundTerminalCommand Start(string id, string command, string workingDirectory)
        {
            (string fileName, string arguments) = ShellCommandRunner.GetShellInvocation(command);
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };

            BackgroundTerminalCommand session = new(id, command, process);
            process.OutputDataReceived += (_, e) => session.AppendStdout(e.Data);
            process.ErrorDataReceived += (_, e) => session.AppendStderr(e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return session;
        }

        public string FormatOutput()
        {
            var status = _process.HasExited ? "completed" : "running";
            int? exitCode = _process.HasExited ? _process.ExitCode : null;

            lock (_sync)
            {
                return $"Id: {Id}\nStatus: {status}\nExitCode: {(exitCode.HasValue ? exitCode.Value.ToString() : "n/a")}\nStartedAtUtc: {StartedAtUtc:O}\nCommand: {Command}\n\nStdout:\n{_stdout.ToString().TrimEnd()}\n\nStderr:\n{_stderr.ToString().TrimEnd()}";
            }
        }

        private void AppendStdout(string? value)
        {
            if (value is null)
            {
                return;
            }

            lock (_sync)
            {
                _stdout.AppendLine(value);
            }
        }

        private void AppendStderr(string? value)
        {
            if (value is null)
            {
                return;
            }

            lock (_sync)
            {
                _stderr.AppendLine(value);
            }
        }
    }
}

