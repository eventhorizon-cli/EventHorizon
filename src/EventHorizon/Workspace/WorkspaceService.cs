using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EventHorizon.Tools;
using EventHorizon.Workspace.Diff;

namespace EventHorizon.Workspace;

public sealed class WorkspaceService : IWorkspaceService
{
    private static readonly HttpClient OsvHttpClient = new();

    private static readonly HashSet<string> IgnoredDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".idea",
        ".vs",
        "artifacts",
        "bin",
        "node_modules",
        "obj",
    };

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp",
        ".class",
        ".dll",
        ".dylib",
        ".exe",
        ".gif",
        ".ico",
        ".jar",
        ".jpeg",
        ".jpg",
        ".nupkg",
        ".o",
        ".pdf",
        ".png",
        ".so",
        ".tar",
        ".tif",
        ".tiff",
        ".zip",
    };

    private static readonly HashSet<string> SearchStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a",
        "an",
        "and",
        "by",
        "code",
        "file",
        "files",
        "find",
        "for",
        "how",
        "in",
        "is",
        "of",
        "or",
        "search",
        "show",
        "the",
        "this",
        "to",
        "tool",
        "where",
    };

    private readonly IWorkspaceContextAccessor _workspaceContextAccessor;
    private readonly ShellCommandRunner _shellCommandRunner;
    private readonly BackgroundTerminalCommandStore _backgroundTerminalCommandStore;
    private readonly IFileSnapshotService _fileSnapshotService;
    private readonly IFileStateTrackerAccessor _fileStateTrackerAccessor;

    public WorkspaceService(
        IWorkspaceContextAccessor workspaceContextAccessor,
        ShellCommandRunner shellCommandRunner,
        IFileSnapshotService fileSnapshotService,
        IFileStateTrackerAccessor fileStateTrackerAccessor,
        BackgroundTerminalCommandStore? backgroundTerminalCommandStore = null)
    {
        _workspaceContextAccessor = workspaceContextAccessor;
        _shellCommandRunner = shellCommandRunner;
        _fileSnapshotService = fileSnapshotService;
        _fileStateTrackerAccessor = fileStateTrackerAccessor;
        _backgroundTerminalCommandStore = backgroundTerminalCommandStore ?? new BackgroundTerminalCommandStore();
    }

    public string WorkspaceRoot => _workspaceContextAccessor.WorkspaceContext.WorkspaceRoot;

    public string DescribeWorkspace()
    {
        StringBuilder builder = new();
        builder.AppendLine($"Workspace root: {WorkspaceRoot}");
        builder.AppendLine("Top-level entries:");
        foreach (var entry in Directory.EnumerateFileSystemEntries(WorkspaceRoot).OrderBy(static p => p, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(entry);
            builder.AppendLine(Directory.Exists(entry) ? $"- {name}/" : $"- {name}");
        }
        return builder.ToString().TrimEnd();
    }

    public string ListDir(string path)
        => ListDirectory(path);

    public string ListDirectory(string? relativePath = null)
    {
        var path = ResolvePath(relativePath);
        StringBuilder builder = new();
        builder.AppendLine($"Directory: {GetDisplayPath(path)}");
        foreach (var entry in Directory.EnumerateFileSystemEntries(path).OrderBy(static p => p, StringComparer.OrdinalIgnoreCase))
        {
            var relative = Path.GetRelativePath(WorkspaceRoot, entry);
            builder.AppendLine(Directory.Exists(entry) ? $"- {relative}/" : $"- {relative}");
        }
        return builder.ToString().TrimEnd();
    }

    public string OpenFile(string filePath, bool isPreview = false)
    {
        var path = ResolvePath(filePath);
        var maxLines = isPreview ? 120 : 250;
        TrackRead(path);
        return $"Opened {GetDisplayPath(path)} (preview={isPreview.ToString().ToLowerInvariant()})\n\n{ReadFile(filePath, 1, maxLines)}";
    }

    public string ReadFileTool(string filePath, int? offset = null, int? limit = null)
        => ReadFile(filePath, offset ?? 1, limit ?? 250);

    public string ReadFile(string relativePath, int startLine = 1, int maxLines = 250)
    {
        var path = ResolvePath(relativePath);
        TrackRead(path);
        var lines = File.ReadAllLines(path);
        var safeStartLine = Math.Max(1, startLine);
        var safeMaxLines = Math.Clamp(maxLines, 1, 500);
        var selected = lines.Skip(safeStartLine - 1).Take(safeMaxLines);
        StringBuilder builder = new();
        var lineNumber = safeStartLine;
        foreach (var line in selected)
        {
            builder.Append(lineNumber++).Append(": ").AppendLine(line);
        }
        return builder.ToString().TrimEnd();
    }

    public string CreateFile(string filePath, string content)
    {
        var path = ResolvePath(filePath);
        if (File.Exists(path))
        {
            throw new InvalidOperationException($"The file '{GetDisplayPath(path)}' already exists.");
        }

        TrackBaseline(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        TrackCurrent(path);
        return $"Created {GetDisplayPath(path)} with {content.Length} characters.";
    }

    public string WriteFile(string relativePath, string content)
    {
        var path = ResolvePath(relativePath);
        TrackBaseline(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        TrackCurrent(path);
        return $"Wrote {content.Length} characters to {GetDisplayPath(path)}.";
    }

    public string AppendFile(string relativePath, string content)
    {
        var path = ResolvePath(relativePath);
        TrackBaseline(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, content);
        TrackCurrent(path);
        return $"Appended {content.Length} characters to {GetDisplayPath(path)}.";
    }

    public string InsertEditIntoFile(string filePath, string searchText, string replacementText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            throw new InvalidOperationException("searchText must be provided for insert_edit_into_file.");
        }

        var path = ResolvePath(filePath);
        TrackBaseline(path);
        var content = File.ReadAllText(path);
        var occurrenceCount = CountOccurrences(content, searchText);
        if (occurrenceCount == 0)
        {
            throw new InvalidOperationException($"The requested text was not found in '{GetDisplayPath(path)}'.");
        }

        if (occurrenceCount > 1)
        {
            throw new InvalidOperationException($"The requested text matched {occurrenceCount} regions in '{GetDisplayPath(path)}'. Provide a more specific snippet.");
        }

        var updated = ReplaceFirstOccurrence(content, searchText, replacementText);
        File.WriteAllText(path, updated);
        TrackCurrent(path);
        return $"Updated 1 region in {GetDisplayPath(path)}.";
    }

    public string ApplyPatch(string filePath, string input, string explanation)
    {
        var normalizedPatch = NormalizePatchEnvelope(filePath, input);
        var operations = ParsePatch(normalizedPatch);
        if (operations.Count == 0)
        {
            throw new InvalidOperationException("No patch operations were provided.");
        }

        foreach (var operation in operations)
        {
            var resolvedPath = ResolvePath(operation.FilePath);
            if (!PathsEqual(resolvedPath, ResolvePath(filePath)))
            {
                throw new InvalidOperationException("apply_patch currently supports exactly one target file per invocation.");
            }

            switch (operation.Kind)
            {
                case PatchOperationKind.Add:
                    if (File.Exists(resolvedPath))
                    {
                        throw new InvalidOperationException($"Cannot add '{GetDisplayPath(resolvedPath)}' because it already exists.");
                    }

                    TrackBaseline(resolvedPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath)!);
                    File.WriteAllText(resolvedPath, string.Join('\n', operation.AddedLines));
                    TrackCurrent(resolvedPath);
                    break;

                case PatchOperationKind.Update:
                    if (!File.Exists(resolvedPath))
                    {
                        throw new InvalidOperationException($"Cannot update '{GetDisplayPath(resolvedPath)}' because it does not exist.");
                    }

                    TrackBaseline(resolvedPath);
                    var sourceSnapshotBeforeUpdate = _fileSnapshotService.CaptureFile(resolvedPath);
                    var currentContent = File.ReadAllText(resolvedPath);
                    var updatedContent = ApplyHunks(currentContent, operation.Hunks, GetDisplayPath(resolvedPath));
                    var destinationPath = operation.MoveToPath is null ? resolvedPath : ResolvePath(operation.MoveToPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    File.WriteAllText(destinationPath, updatedContent);
                    if (!PathsEqual(destinationPath, resolvedPath))
                    {
                        File.Delete(resolvedPath);
                        TrackRename(resolvedPath, destinationPath, sourceSnapshotBeforeUpdate);
                    }
                    else
                    {
                        TrackCurrent(destinationPath);
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Patch operation '{operation.Kind}' is not supported.");
            }
        }

        return $"Applied patch to {GetDisplayPath(ResolvePath(filePath))}. Explanation: {explanation}";
    }

    public string FileSearch(string query, int maxResults = 200)
    {
        var regex = WildcardToRegex(query);
        var matches = EnumerateWorkspaceFiles()
            .Select(path => Path.GetRelativePath(WorkspaceRoot, path))
            .Where(path => regex.IsMatch(path))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(maxResults, 1, 500))
            .ToList();

        return matches.Count == 0 ? "No files matched the query." : string.Join(Environment.NewLine, matches);
    }

    public string FindFiles(string pattern = "*")
        => FileSearch(pattern, 500);

    public string GrepSearch(string query, bool isRegexp = false, string includePattern = "*")
    {
        var fileRegex = WildcardToRegex(includePattern);
        Regex searchRegex = new(isRegexp ? query : Regex.Escape(query), RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        List<string> matches = [];
        foreach (var file in EnumerateWorkspaceFiles())
        {
            var relative = Path.GetRelativePath(WorkspaceRoot, file);
            if (!fileRegex.IsMatch(relative))
            {
                continue;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch
            {
                continue;
            }

            for (var i = 0; i < lines.Length; i++)
            {
                if (searchRegex.IsMatch(lines[i]))
                {
                    matches.Add($"{relative}:{i + 1}: {lines[i]}");
                }
            }
        }

        return matches.Count == 0 ? "No matches found." : string.Join(Environment.NewLine, matches.Take(500));
    }

    public string Grep(string pattern, string filePattern = "*")
        => GrepSearch(pattern, isRegexp: true, includePattern: filePattern);

    public string SemanticSearch(string query, int maxResults = 8)
    {
        var terms = ExtractSearchTerms(query);
        if (terms.Length == 0)
        {
            return "No semantic search terms were extracted from the query.";
        }

        List<SemanticSnippet> snippets = [];
        foreach (var file in EnumerateWorkspaceFiles())
        {
            var relative = Path.GetRelativePath(WorkspaceRoot, file);
            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch
            {
                continue;
            }

            var fileBonus = ScoreText(relative, terms) * 3;
            for (var i = 0; i < lines.Length; i++)
            {
                var lineScore = ScoreText(lines[i], terms);
                if (lineScore == 0 && fileBonus == 0)
                {
                    continue;
                }

                var start = Math.Max(0, i - 2);
                var end = Math.Min(lines.Length - 1, i + 2);
                snippets.Add(new SemanticSnippet(relative, start + 1, end + 1, fileBonus + lineScore, BuildSnippet(lines, start, end)));
            }
        }

        var ranked = snippets
            .OrderByDescending(static snippet => snippet.Score)
            .ThenBy(static snippet => snippet.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static snippet => snippet.StartLine)
            .DistinctBy(static snippet => (snippet.Path, snippet.StartLine, snippet.EndLine))
            .Take(Math.Clamp(maxResults, 1, 32))
            .ToArray();

        if (ranked.Length == 0)
        {
            return "No semantic matches found.";
        }

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            ranked.Select(static snippet => $"{snippet.Path}:{snippet.StartLine}-{snippet.EndLine} (score={snippet.Score})\n{snippet.Snippet}"));
    }

    public async Task<string> RunInTerminalAsync(string command, string explanation, bool isBackground, CancellationToken cancellationToken)
    {
        if (isBackground)
        {
            var id = _backgroundTerminalCommandStore.Start(command, WorkspaceRoot);
            return $"Started background terminal session.\nId: {id}\nExplanation: {explanation}\nCommand: {command}";
        }

        var beforeSnapshot = CaptureWorkspaceForTracking();
        var result = await _shellCommandRunner.RunAsync(command, WorkspaceRoot, 120, cancellationToken).ConfigureAwait(false);
        TrackWorkspaceTransition(beforeSnapshot, CaptureWorkspaceForTracking());
        return $"Explanation: {explanation}\n{result}";
    }

    public string GetTerminalOutput(string id)
        => _backgroundTerminalCommandStore.GetOutput(id);

    public async Task<string> RunShellAsync(string command, int timeoutSeconds, CancellationToken cancellationToken)
    {
        var result = await _shellCommandRunner.RunAsync(command, WorkspaceRoot, timeoutSeconds, cancellationToken).ConfigureAwait(false);
        return result.ToString();
    }

    public async Task<string> GetErrorsAsync(string[] filePaths, CancellationToken cancellationToken)
    {
        if (filePaths.Length == 0)
        {
            return "No files were supplied.";
        }

        var buildTarget = FindDotNetBuildTarget();
        if (buildTarget is null)
        {
            return "No .NET solution or project was found for diagnostics.";
        }

        var requestedPaths = filePaths
            .Select(ResolvePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = await _shellCommandRunner
            .RunAsync($"dotnet build \"{buildTarget}\" --nologo --no-restore", WorkspaceRoot, 180, cancellationToken)
            .ConfigureAwait(false);

        var combinedOutput = string.Join(
            Environment.NewLine,
            new[] { result.StandardOutput, result.StandardError }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        List<string> matches = [];
        Regex regex = new(@"^(?<path>.+?)\((?<line>\d+)(,(?<column>\d+))?\): (?<severity>error|warning) (?<code>[^:]+): (?<message>.+)$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
        foreach (Match match in regex.Matches(combinedOutput))
        {
            var diagnosticPath = Path.GetFullPath(match.Groups["path"].Value);
            if (!requestedPaths.Contains(diagnosticPath))
            {
                continue;
            }

            matches.Add(match.Value.Trim());
        }

        if (matches.Count > 0)
        {
            return string.Join(Environment.NewLine, matches);
        }

        if (result.ExitCode == 0)
        {
            return "No diagnostics found for the requested files.";
        }

        return $"Build completed with no matched file diagnostics.\n{result}";
    }

    public async Task<string> ValidateCvesAsync(string[] dependencies, string ecosystem, CancellationToken cancellationToken)
    {
        if (dependencies.Length == 0)
        {
            return "No dependencies were supplied.";
        }

        var mappedEcosystem = MapEcosystem(ecosystem);
        OsvBatchQueryRequest request = new(
            dependencies.Select(dependency => ParseDependency(dependency, mappedEcosystem)).ToArray());

        using HttpRequestMessage message = new(HttpMethod.Post, "https://api.osv.dev/v1/querybatch")
        {
            Content = new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), Encoding.UTF8, "application/json"),
        };

        OsvBatchQueryResponse? payload;
        try
        {
            using var response = await OsvHttpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            payload = JsonSerializer.Deserialize<OsvBatchQueryResponse>(content, JsonSerializerOptions.Web);
        }
        catch (HttpRequestException)
        {
            return "Unable to reach the OSV service right now. Please try again later.";
        }

        if (payload?.Results is null || payload.Results.Length == 0)
        {
            return "No vulnerability data was returned.";
        }

        StringBuilder builder = new();
        for (var i = 0; i < dependencies.Length; i++)
        {
            builder.AppendLine(dependencies[i]);
            var result = payload.Results.ElementAtOrDefault(i) ?? new OsvBatchResult(null);
            if (result.Vulns is null || result.Vulns.Length == 0)
            {
                builder.AppendLine("- No known vulnerabilities found.");
                continue;
            }

            foreach (var vulnerability in result.Vulns)
            {
                var fixedVersions = string.Join(", ", vulnerability.Affected?
                    .SelectMany(static affected => affected.Ranges ?? [])
                    .SelectMany(static range => range.Events ?? [])
                    .Select(static @event => @event.Fixed)
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    ?? []);

                builder.Append("- ").Append(vulnerability.Id ?? "unknown-id");
                if (!string.IsNullOrWhiteSpace(vulnerability.Summary))
                {
                    builder.Append(": ").Append(vulnerability.Summary);
                }

                builder.AppendLine();
                if (!string.IsNullOrWhiteSpace(fixedVersions))
                {
                    builder.AppendLine($"  Fixed in: {fixedVersions}");
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    public string AskQuestions(AskQuestionDefinition[] questions)
    {
        if (questions.Length == 0)
        {
            return "No questions were supplied.";
        }

        StringBuilder builder = new();
        builder.AppendLine("Prepared question bundle:");
        foreach (var question in questions)
        {
            builder.Append("- ").Append(question.Header).Append(": ").AppendLine(question.Question);
            builder.Append("  multiSelect=").Append(question.MultiSelect.ToString().ToLowerInvariant())
                .Append(", allowFreeformInput=")
                .AppendLine(question.AllowFreeformInput.ToString().ToLowerInvariant());

            if (question.Options is not null)
            {
                foreach (var option in question.Options)
                {
                    builder.Append("  - ").Append(option.Label);
                    if (!string.IsNullOrWhiteSpace(option.Description))
                    {
                        builder.Append(" — ").Append(option.Description);
                    }

                    builder.AppendLine();
                }
            }
        }

        builder.AppendLine("Use these questions in your next response and wait for the user's reply.");
        return builder.ToString().TrimEnd();
    }

    public string RunSubagent(string task, string agentName, string? description = null)
    {
        if (!string.Equals(agentName, "Search", StringComparison.OrdinalIgnoreCase))
        {
            return $"Subagent '{agentName}' is not available. Supported agents: Search.";
        }

        var terms = ExtractSearchTerms(task);
        var fileCandidates = EnumerateWorkspaceFiles()
            .Select(path => Path.GetRelativePath(WorkspaceRoot, path))
            .Where(path => terms.Any(term => path.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        var semanticResults = SemanticSearch(task, 5);
        StringBuilder builder = new();
        builder.AppendLine("Subagent: Search");
        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.AppendLine($"Description: {description}");
        }

        builder.AppendLine($"Task: {task}");
        builder.AppendLine();
        builder.AppendLine("Candidate files:");
        if (fileCandidates.Count == 0)
        {
            builder.AppendLine("- No file candidates found.");
        }
        else
        {
            foreach (var candidate in fileCandidates)
            {
                builder.Append("- ").AppendLine(candidate);
            }
        }

        builder.AppendLine();
        builder.AppendLine("Semantic findings:");
        builder.AppendLine(semanticResults);
        return builder.ToString().TrimEnd();
    }

    private string ResolvePath(string? path)
    {
        var candidate = string.IsNullOrWhiteSpace(path)
            ? WorkspaceRoot
            : Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(WorkspaceRoot, path));

        if (!IsInsideWorkspace(candidate))
        {
            throw new InvalidOperationException("The requested path escapes the configured workspace root.");
        }

        return candidate;
    }

    private bool IsInsideWorkspace(string path)
    {
        var rootWithSeparator = WorkspaceRoot.EndsWith(Path.DirectorySeparatorChar)
            ? WorkspaceRoot
            : WorkspaceRoot + Path.DirectorySeparatorChar;

        return path.Equals(WorkspaceRoot, StringComparison.Ordinal)
            || path.StartsWith(rootWithSeparator, StringComparison.Ordinal);
    }

    private string GetDisplayPath(string fullPath)
    {
        var relative = Path.GetRelativePath(WorkspaceRoot, fullPath);
        return relative == "." ? "." : relative;
    }

    private IEnumerable<string> EnumerateWorkspaceFiles()
    {
        return Directory.EnumerateFiles(WorkspaceRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path) && !BinaryExtensions.Contains(Path.GetExtension(path)));
    }

    private bool IsIgnoredPath(string path)
    {
        var relative = Path.GetRelativePath(WorkspaceRoot, path);
        var segments = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(IgnoredDirectoryNames.Contains);
    }

    private static int CountOccurrences(string content, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = content.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string ReplaceFirstOccurrence(string content, string searchText, string replacementText)
    {
        var index = content.IndexOf(searchText, StringComparison.Ordinal);
        return content[..index] + replacementText + content[(index + searchText.Length)..];
    }

    private static string NormalizePatchEnvelope(string filePath, string input)
    {
        var normalized = input.Replace("\r\n", "\n");
        if (normalized.Contains("*** Begin Patch", StringComparison.Ordinal))
        {
            return normalized;
        }

        var patchPath = Path.IsPathRooted(filePath) ? filePath : filePath.Replace('\\', '/');
        return $"*** Begin Patch\n*** Update File: {patchPath}\n@@\n{normalized}\n*** End Patch";
    }

    private List<PatchOperation> ParsePatch(string patch)
    {
        var lines = patch.Replace("\r\n", "\n").Split('\n');
        if (lines.Length < 2 || !string.Equals(lines[0], "*** Begin Patch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Patch input must start with '*** Begin Patch'.");
        }

        List<PatchOperation> operations = [];
        var index = 1;
        while (index < lines.Length)
        {
            var line = lines[index];
            if (string.Equals(line, "*** End Patch", StringComparison.Ordinal))
            {
                break;
            }

            if (line.StartsWith("*** Add File: ", StringComparison.Ordinal))
            {
                var operationPath = line[14..].Trim();
                index++;
                List<string> addedLines = [];
                while (index < lines.Length && !lines[index].StartsWith("*** ", StringComparison.Ordinal))
                {
                    if (!lines[index].StartsWith('+'))
                    {
                        throw new InvalidOperationException("Add file patches may only contain '+' lines.");
                    }

                    addedLines.Add(lines[index][1..]);
                    index++;
                }

                operations.Add(PatchOperation.Add(operationPath, addedLines));
                continue;
            }

            if (!line.StartsWith("*** Update File: ", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unsupported patch header '{line}'.");
            }

            var filePathValue = line[17..].Trim();
            index++;
            string? moveToPath = null;
            if (index < lines.Length && lines[index].StartsWith("*** Move to:", StringComparison.Ordinal))
            {
                moveToPath = lines[index][12..].Trim();
                index++;
            }

            List<PatchHunk> hunks = [];
            while (index < lines.Length && !lines[index].StartsWith("*** ", StringComparison.Ordinal))
            {
                if (!lines[index].StartsWith("@@", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Expected a hunk header but found '{lines[index]}'.");
                }

                index++;
                List<PatchLine> patchLines = [];
                while (index < lines.Length && !lines[index].StartsWith("@@", StringComparison.Ordinal) && !lines[index].StartsWith("*** ", StringComparison.Ordinal))
                {
                    var patchLine = lines[index];
                    if (string.Equals(patchLine, "*** End of File", StringComparison.Ordinal))
                    {
                        index++;
                        break;
                    }

                    if (patchLine.Length == 0)
                    {
                        patchLines.Add(new PatchLine(' ', string.Empty));
                    }
                    else
                    {
                        var prefix = patchLine[0];
                        if (prefix is not (' ' or '+' or '-'))
                        {
                            throw new InvalidOperationException($"Unsupported patch line '{patchLine}'.");
                        }

                        patchLines.Add(new PatchLine(prefix, patchLine[1..]));
                    }

                    index++;
                }

                hunks.Add(new PatchHunk(patchLines));
            }

            operations.Add(PatchOperation.Update(filePathValue, moveToPath, hunks));
        }

        return operations;
    }

    private static string ApplyHunks(string content, IReadOnlyList<PatchHunk> hunks, string displayPath)
    {
        var hadTrailingNewline = content.EndsWith('\n');
        var lines = content.Replace("\r\n", "\n").Split('\n').ToList();
        if (hadTrailingNewline && lines.Count > 0 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        var searchStart = 0;
        foreach (var hunk in hunks)
        {
            var before = hunk.Lines.Where(static line => line.Prefix != '+').Select(static line => line.Text).ToList();
            var after = hunk.Lines.Where(static line => line.Prefix != '-').Select(static line => line.Text).ToList();
            if (before.Count == 0)
            {
                throw new InvalidOperationException("Patch hunks must include at least one context or removed line.");
            }

            var index = FindSequence(lines, before, searchStart);
            if (index < 0)
            {
                throw new InvalidOperationException($"Failed to apply patch to '{displayPath}' because the expected context was not found.");
            }

            lines.RemoveRange(index, before.Count);
            lines.InsertRange(index, after);
            searchStart = index;
        }

        var updated = string.Join('\n', lines);
        return hadTrailingNewline ? updated + '\n' : updated;
    }

    private static int FindSequence(List<string> lines, List<string> sequence, int startIndex)
    {
        if (sequence.Count == 0)
        {
            return startIndex;
        }

        for (var i = Math.Max(0, startIndex); i <= lines.Count - sequence.Count; i++)
        {
            var matched = true;
            for (var j = 0; j < sequence.Count; j++)
            {
                if (!string.Equals(lines[i + j], sequence[j], StringComparison.Ordinal))
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return i;
            }
        }

        return -1;
    }

    private string? FindDotNetBuildTarget()
    {
        var slnx = Directory.EnumerateFiles(WorkspaceRoot, "*.slnx", SearchOption.TopDirectoryOnly).OrderBy(static path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        if (slnx is not null)
        {
            return Path.GetRelativePath(WorkspaceRoot, slnx);
        }

        var sln = Directory.EnumerateFiles(WorkspaceRoot, "*.sln", SearchOption.TopDirectoryOnly).OrderBy(static path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        if (sln is not null)
        {
            return Path.GetRelativePath(WorkspaceRoot, sln);
        }

        var project = Directory.EnumerateFiles(WorkspaceRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        return project is null ? null : Path.GetRelativePath(WorkspaceRoot, project);
    }

    private static string[] ExtractSearchTerms(string query)
    {
        return Regex.Matches(query, @"[A-Za-z0-9_\.:-]{3,}")
            .Select(static match => match.Value)
            .Where(term => !SearchStopWords.Contains(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
    }

    private static int ScoreText(string value, IEnumerable<string> terms)
    {
        var score = 0;
        foreach (var term in terms)
        {
            if (value.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += term.Length;
            }
        }

        return score;
    }

    private static string BuildSnippet(IReadOnlyList<string> lines, int start, int end)
    {
        StringBuilder builder = new();
        for (var i = start; i <= end; i++)
        {
            builder.Append(i + 1).Append(": ").AppendLine(lines[i]);
        }

        return builder.ToString().TrimEnd();
    }

    private static string MapEcosystem(string ecosystem)
        => ecosystem.Trim().ToLowerInvariant() switch
        {
            "actions" => "GitHub Actions",
            "composer" => "Packagist",
            "erlang" => "Hex",
            "go" => "Go",
            "maven" => "Maven",
            "npm" => "npm",
            "nuget" => "NuGet",
            "pip" => "PyPI",
            "pub" => "Pub",
            "rubygems" => "RubyGems",
            "rust" => "crates.io",
            _ => throw new InvalidOperationException($"Unsupported ecosystem '{ecosystem}'."),
        };

    private static OsvQuery ParseDependency(string dependency, string ecosystem)
    {
        var separatorIndex = dependency.LastIndexOf('@');
        if (separatorIndex <= 0 || separatorIndex == dependency.Length - 1)
        {
            throw new InvalidOperationException($"Dependency '{dependency}' must be in the format package@version.");
        }

        return new OsvQuery(new OsvPackage(dependency[..separatorIndex], ecosystem), dependency[(separatorIndex + 1)..]);
    }

    private static bool PathsEqual(string left, string right)
        => string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static Regex WildcardToRegex(string pattern)
    {
        var escaped = Regex.Escape(string.IsNullOrWhiteSpace(pattern) ? "*" : pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".");
        return new Regex("^" + escaped + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private sealed record SemanticSnippet(string Path, int StartLine, int EndLine, int Score, string Snippet);

    private sealed record PatchLine(char Prefix, string Text);

    private sealed record PatchHunk(IReadOnlyList<PatchLine> Lines);

    private enum PatchOperationKind
    {
        Add,
        Update,
    }

    private WorkspaceSnapshot? CaptureWorkspaceForTracking()
        => _fileStateTrackerAccessor.Current is null ? null : _fileSnapshotService.CaptureWorkspace();

    private void TrackRead(string path)
        => _fileStateTrackerAccessor.Current?.RecordRead(path);

    private void TrackBaseline(string path)
        => _fileStateTrackerAccessor.Current?.CaptureBaseline(path);

    private void TrackCurrent(string path)
        => _fileStateTrackerAccessor.Current?.CaptureCurrent(path);

    private void TrackRename(string oldPath, string newPath, FileSnapshot? sourceSnapshotBeforeRename)
        => _fileStateTrackerAccessor.Current?.RecordRename(oldPath, newPath, sourceSnapshotBeforeRename);

    private void TrackWorkspaceTransition(WorkspaceSnapshot? beforeSnapshot, WorkspaceSnapshot? afterSnapshot)
    {
        if (beforeSnapshot is null || afterSnapshot is null)
        {
            return;
        }

        _fileStateTrackerAccessor.Current?.RecordWorkspaceTransition(beforeSnapshot, afterSnapshot);
    }

    private sealed record PatchOperation(PatchOperationKind Kind, string FilePath, string? MoveToPath, IReadOnlyList<string> AddedLines, IReadOnlyList<PatchHunk> Hunks)
    {
        public static PatchOperation Add(string filePath, IReadOnlyList<string> addedLines)
            => new(PatchOperationKind.Add, filePath, null, addedLines, []);

        public static PatchOperation Update(string filePath, string? moveToPath, IReadOnlyList<PatchHunk> hunks)
            => new(PatchOperationKind.Update, filePath, moveToPath, [], hunks);
    }

    private sealed record OsvBatchQueryRequest(OsvQuery[] Queries);

    private sealed record OsvQuery(OsvPackage Package, string Version);

    private sealed record OsvPackage(string Name, string Ecosystem);

    private sealed record OsvBatchQueryResponse(OsvBatchResult[] Results);

    private sealed record OsvBatchResult(OsvVulnerability[]? Vulns);

    private sealed record OsvVulnerability(string? Id, string? Summary, OsvAffected[]? Affected);

    private sealed record OsvAffected(OsvRange[]? Ranges);

    private sealed record OsvRange(OsvEvent[]? Events);

    private sealed record OsvEvent(string? Introduced, string? Fixed);
}
