using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Agents.AI;

namespace EventHorizon.Workspace.Skills;

/// <summary>
/// A skill that helps the agent solve complex workspace tasks by generating and
/// running .NET 10 file-based C# apps.
/// </summary>
internal sealed class DotNetFileRunnerSkill : AgentClassSkill<DotNetFileRunnerSkill>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    /// <inheritdoc/>
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "dotnet-file-runner",
        "Use .NET 10 file-based C# apps to solve complex workspace tasks. Useful for code analysis, data transformation, migration planning, report generation, validation, and multi-file automation.");

    /// <inheritdoc/>
    protected override JsonSerializerOptions? SerializerOptions => JsonOptions;

    /// <inheritdoc/>
    protected override string Instructions => """
                                              Use this skill when the user asks for a task that benefits from real program execution,
                                              deterministic analysis, multi-file processing, data transformation, code migration,
                                              validation, report generation, or complex workspace automation.

                                              This skill works best with the available workspace tools:

                                              - list_dir: inspect directories.
                                              - file_search: locate files by name.
                                              - grep_search: search exact text or regex-like patterns.
                                              - semantic_search: find conceptually relevant code.
                                              - open_file / read_file: inspect file contents.
                                              - create_file: create a temporary .NET file-based app.
                                              - replace_string_in_file / insert_edit_into_file / apply_patch: apply edits after validation.
                                              - validate_cves: validate CVE references if security findings are involved.

                                              Preferred workflow:

                                              1. Understand the user's goal.
                                              2. Inspect the workspace with list_dir, file_search, grep_search, semantic_search,
                                                 open_file, and read_file.
                                              3. If the task is complex, create a temporary C# file-based app, usually under:
                                                 .eventhorizon/file-apps/<task-name>.cs

                                              4. Use the dotnet-file-app-template resource as the starting point.
                                                 The generated app should:
                                                 - Be deterministic.
                                                 - Use top-level statements.
                                                 - Prefer JSON input/output for machine readability.
                                                 - Read files from the current working directory, which is the workspace root.
                                                 - Prefer explicit paths relative to the workspace root.
                                                 - Avoid modifying user files directly unless explicitly intended.
                                                 - Print a final JSON object to stdout.

                                              5. Run the app with run_dotnet_file.
                                                 The runner deletes the generated .cs file after execution by default.
                                              6. If compilation or runtime errors occur:
                                                 - Read stderr carefully.
                                                 - Recreate or fix the generated .cs file using workspace editing tools.
                                                 - Run again.

                                              7. Only after the generated app has produced a validated result should you edit
                                                 the user's real files with apply_patch, replace_string_in_file, or
                                                 insert_edit_into_file.

                                              8. For code modification tasks:
                                                 - Prefer generating a plan or patch first.
                                                 - Explain the changes.
                                                 - Apply the smallest safe patch.
                                                 - Re-read or validate changed files when possible.

                                              9. For security-related tasks:
                                                 - Use validate_cves for CVE identifiers.
                                                 - Do not invent CVEs.
                                                 - Clearly separate verified findings from recommendations.

                                              Do not use this skill for trivial tasks that can be answered directly.
                                              Use this skill when executing C# code will improve correctness, speed, or reliability.
                                              """;

    /// <summary>
    /// A recommended template for generated .NET 10 file-based apps.
    /// </summary>
    [AgentSkillResource("dotnet-file-app-template")]
    [Description("Template for a .NET 10 file-based C# app used for complex workspace tasks.")]
    public string DotNetFileAppTemplate => """
                                           // <task-name>.cs
                                           // Run with:
                                           //   dotnet run --file <task-name>.cs
                                           //
                                           // Optional examples:
                                           //   #:package System.CommandLine@2.0.0-beta7.25380.108
                                           //   #:property LangVersion=preview
                                           //   #:property Nullable=enable

                                           #:property LangVersion=preview
                                           #:property Nullable=enable
                                           #:property PublishAot=false

                                           using System.Text.Json;
                                           using System.Text.Json.Serialization;
                                           using System.Text.Json.Serialization.Metadata;
                                           using System.Text.RegularExpressions;

                                           var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                                           {
                                               WriteIndented = true,
                                               DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                                               TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                                           };

                                           string workspaceRoot = Directory.GetCurrentDirectory();

                                           // Keep argument parsing simple and deterministic.
                                           // The agent can call:
                                           //   dotnet run --file task.cs -- path/to/file.ext
                                           string[] inputArgs = args;

                                           object result;

                                           try
                                           {
                                               // TODO:
                                               // 1. Read input files.
                                               // 2. Perform deterministic analysis/transformation.
                                               // 3. Return a JSON-serializable result.
                                               //
                                               // Example:
                                               // var targetFile = inputArgs.FirstOrDefault();
                                               // var text = targetFile is null ? "" : await File.ReadAllTextAsync(Path.Combine(workspaceRoot, targetFile));

                                               result = new
                                               {
                                                   ok = true,
                                                   workspaceRoot,
                                                   args = inputArgs,
                                                   message = "Replace this template with task-specific logic."
                                               };
                                           }
                                           catch (Exception ex)
                                           {
                                               result = new
                                               {
                                                   ok = false,
                                                   error = ex.GetType().FullName,
                                                   message = ex.Message,
                                                   stackTrace = ex.StackTrace
                                               };
                                           }

                                           Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
                                           """;

    /// <summary>
    /// Guidance for using workspace tools with this skill.
    /// </summary>
    [AgentSkillResource("workspace-tool-strategy")]
    [Description("Recommended strategy for combining workspace tools with .NET file-based task apps.")]
    public string WorkspaceToolStrategy => """
                                           # Workspace Tool Strategy

                                           Use this sequence for complex tasks:

                                           ## Discovery

                                           - list_dir: understand top-level structure.
                                           - file_search: locate files by name, extension, or convention.
                                           - grep_search: find exact symbols, APIs, configuration keys, or error messages.
                                           - semantic_search: find related implementations when exact terms are unknown.
                                           - open_file/read_file: inspect candidate files.

                                           ## Execution

                                           - create_file: create .eventhorizon/file-apps/<task>.cs.
                                           - run_dotnet_file: execute the generated .NET 10 file-based app and delete the generated .cs file after execution.
                                           - Iterate until the app produces reliable JSON output.

                                           ## Editing

                                           - Prefer apply_patch for multi-file or structured edits.
                                           - Prefer replace_string_in_file for exact localized replacement.
                                           - Prefer insert_edit_into_file for inserting code into known locations.
                                           - Re-read changed files after edits.

                                           ## Safety

                                           - Generated task apps should usually be read-only.
                                           - Do not mutate user files from the generated app unless explicitly requested.
                                           - Let the agent apply final edits through workspace tools so changes are visible and reviewable.
                                           """;

    /// <summary>
    /// Gets .NET SDK information from the current environment.
    /// </summary>
    [AgentSkillScript("dotnet_info")]
    [Description("Returns dotnet --info and installed SDK information as JSON.")]
    private static async Task<string> GetDotNetInfoAsync(
        IServiceProvider serviceProvider,
        int timeoutSeconds = 30,
        int maxOutputChars = 20_000)
    {
        var workingDirectory = GetWorkspaceRoot(serviceProvider);

        var info = await RunProcessAsync(
            fileName: "dotnet",
            arguments: ["--info"],
            workingDirectory,
            timeoutSeconds).ConfigureAwait(false);

        var sdks = await RunProcessAsync(
            fileName: "dotnet",
            arguments: ["--list-sdks"],
            workingDirectory,
            timeoutSeconds).ConfigureAwait(false);

        return ToJson(new { dotnetInfo = Truncate(info, maxOutputChars), sdks = Truncate(sdks, maxOutputChars), });
    }

    /// <summary>
    /// Builds a .NET 10 file-based C# app without running application logic.
    /// </summary>
    [AgentSkillScript("build_dotnet_file")]
    [Description("Builds a .NET 10 file-based C# app and returns stdout, stderr, exit code, and timing as JSON.")]
    private static async Task<string> BuildDotNetFileAsync(
        IServiceProvider serviceProvider,
        string filePath,
        string? workingDirectory = null,
        int timeoutSeconds = 120,
        int maxOutputChars = 40_000)
    {
        var fullWorkingDirectory = ResolveWorkingDirectory(serviceProvider, workingDirectory);
        var fullFilePath = ResolveFilePathInsideWorkingDirectory(fullWorkingDirectory, filePath);

        var result = await RunProcessAsync(
            fileName: "dotnet",
            arguments: ["build", fullFilePath],
            workingDirectory: fullWorkingDirectory,
            timeoutSeconds).ConfigureAwait(false);

        return ToJson(new
        {
            command = "dotnet build",
            file = fullFilePath,
            workingDirectory = fullWorkingDirectory,
            result = Truncate(result, maxOutputChars),
        });
    }

    /// <summary>
    /// Runs a .NET 10 file-based C# app.
    /// </summary>
    [AgentSkillScript("run_dotnet_file")]
    [Description(
        "Runs a .NET 10 file-based C# app, deletes the generated .cs file by default, and returns stdout, stderr, exit code, timing, and cleanup status as JSON.")]
    private static async Task<string> RunDotNetFileAsync(
        IServiceProvider serviceProvider,
        string filePath,
        string? workingDirectory = null,
        string[]? applicationArguments = null,
        bool deleteAfterRun = true,
        int timeoutSeconds = 180,
        int maxOutputChars = 80_000)
    {
        var fullWorkingDirectory = ResolveWorkingDirectory(serviceProvider, workingDirectory);
        var fullFilePath = ResolveFilePathInsideWorkingDirectory(fullWorkingDirectory, filePath);

        List<string> arguments = ["run", "--file", fullFilePath];

        if (applicationArguments is { Length: > 0 })
        {
            // Separate dotnet CLI arguments from the app's own arguments.
            arguments.Add("--");
            arguments.AddRange(applicationArguments);
        }

        var result = await RunProcessAsync(
            fileName: "dotnet",
            arguments,
            workingDirectory: fullWorkingDirectory,
            timeoutSeconds).ConfigureAwait(false);

        var cleanup = deleteAfterRun
            ? DeleteFileBestEffort(fullFilePath)
            : FileCleanupResult.CreateSkipped(fullFilePath);

        return ToJson(new
        {
            command = "dotnet run",
            file = fullFilePath,
            workingDirectory = fullWorkingDirectory,
            applicationArguments = applicationArguments ?? [],
            result = Truncate(result, maxOutputChars),
            cleanup,
        });
    }

    private static FileCleanupResult DeleteFileBestEffort(string filePath)
    {
        try
        {
            File.Delete(filePath);
            return new FileCleanupResult(filePath, Deleted: true, Skipped: false, Error: null);
        }
        catch (Exception ex)
        {
            return new FileCleanupResult(filePath, Deleted: false, Skipped: false, Error: ex.Message);
        }
    }

    private static string ResolveWorkingDirectory(IServiceProvider serviceProvider, string? workingDirectory)
    {
        var directory = string.IsNullOrWhiteSpace(workingDirectory)
            ? GetWorkspaceRoot(serviceProvider)
            : workingDirectory;

        var fullDirectory = Path.GetFullPath(directory);

        if (!Directory.Exists(fullDirectory))
        {
            throw new DirectoryNotFoundException($"Working directory does not exist: {fullDirectory}");
        }

        return fullDirectory;
    }

    private static string GetWorkspaceRoot(IServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<IWorkspaceContextAccessor>().WorkspaceContext.WorkspaceRoot;

    private static string ResolveFilePathInsideWorkingDirectory(string workingDirectory, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        var fullPath = Path.GetFullPath(
            Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(workingDirectory, filePath));

        var fullWorkingDirectory = Path.GetFullPath(workingDirectory);
        var normalizedWorkingDirectory = EnsureTrailingDirectorySeparator(fullWorkingDirectory);

        if (!fullPath.StartsWith(normalizedWorkingDirectory, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, fullWorkingDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The file must be inside the working directory. File: {fullPath}; Working directory: {fullWorkingDirectory}");
        }

        if (!string.Equals(Path.GetExtension(fullPath), ".cs", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Only .cs file-based apps can be executed. File: {fullPath}");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File-based app does not exist: {fullPath}", fullPath);
        }

        return fullPath;
    }

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        return Path.EndsInDirectorySeparator(path)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static async Task<ProcessRunResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        int timeoutSeconds)
    {
        timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 900);

        using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(timeoutSeconds));

        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;

        StringBuilder stdout = new();
        StringBuilder stderr = new();
        var stopwatch = Stopwatch.StartNew();

        var timedOut = false;

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdout.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stderr.AppendLine(e.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start process: {fileName}");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            timedOut = true;

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best-effort process cleanup.
            }

            try
            {
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Ignore cleanup wait failures.
            }
        }

        stopwatch.Stop();

        var exitCode = process.HasExited ? process.ExitCode : -1;

        return new ProcessRunResult(
            FileName: fileName,
            Arguments: arguments.ToArray(),
            WorkingDirectory: workingDirectory,
            ExitCode: exitCode,
            TimedOut: timedOut,
            ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
            Stdout: stdout.ToString(),
            Stderr: stderr.ToString());
    }

    private static object Truncate(ProcessRunResult result, int maxOutputChars)
    {
        maxOutputChars = Math.Clamp(maxOutputChars, 1_000, 500_000);

        return new
        {
            result.FileName,
            result.Arguments,
            result.WorkingDirectory,
            result.ExitCode,
            result.TimedOut,
            result.ElapsedMilliseconds,
            stdout = TruncateString(result.Stdout, maxOutputChars),
            stderr = TruncateString(result.Stderr, maxOutputChars),
            stdoutTruncated = result.Stdout.Length > maxOutputChars,
            stderrTruncated = result.Stderr.Length > maxOutputChars,
        };
    }

    private static string TruncateString(string value, int maxChars)
    {
        if (value.Length <= maxChars)
        {
            return value;
        }

        return value[..maxChars] + "\n\n...[truncated]...";
    }

    private static string ToJson(object value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private sealed record ProcessRunResult(
        string FileName,
        IReadOnlyList<string> Arguments,
        string WorkingDirectory,
        int ExitCode,
        bool TimedOut,
        long ElapsedMilliseconds,
        string Stdout,
        string Stderr);

    private sealed record FileCleanupResult(
        string FilePath,
        bool Deleted,
        bool Skipped,
        string? Error)
    {
        public static FileCleanupResult CreateSkipped(string filePath) =>
            new(filePath, Deleted: false, Skipped: true, Error: null);
    }
}
