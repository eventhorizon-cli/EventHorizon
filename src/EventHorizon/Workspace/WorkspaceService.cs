using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EventHorizon.Tools;

namespace EventHorizon.Workspace;

public sealed class WorkspaceService
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

    private readonly string _workspaceRoot;
    private readonly ShellCommandRunner _shellCommandRunner;
    private readonly BackgroundTerminalCommandStore _backgroundTerminalCommandStore;

    public WorkspaceService(string workspaceRoot, ShellCommandRunner shellCommandRunner, BackgroundTerminalCommandStore? backgroundTerminalCommandStore = null)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
        _shellCommandRunner = shellCommandRunner;
        _backgroundTerminalCommandStore = backgroundTerminalCommandStore ?? new BackgroundTerminalCommandStore();
    }

    public string WorkspaceRoot => _workspaceRoot;

    public string DescribeWorkspace()
    {
        StringBuilder builder = new();
        builder.AppendLine($"Workspace root: {_workspaceRoot}");
        builder.AppendLine("Top-level entries:");
        foreach (string entry in Directory.EnumerateFileSystemEntries(_workspaceRoot).OrderBy(static p => p, StringComparer.OrdinalIgnoreCase))
        {
            string name = Path.GetFileName(entry);
            builder.AppendLine(Directory.Exists(entry) ? $"- {name}/" : $"- {name}");
        }
        return builder.ToString().TrimEnd();
    }

    public string ListDir(string path)
        => ListDirectory(path);

    public string ListDirectory(string? relativePath = null)
    {
        string path = ResolvePath(relativePath);
        StringBuilder builder = new();
        builder.AppendLine($"Directory: {GetDisplayPath(path)}");
        foreach (string entry in Directory.EnumerateFileSystemEntries(path).OrderBy(static p => p, StringComparer.OrdinalIgnoreCase))
        {
            string relative = Path.GetRelativePath(_workspaceRoot, entry);
            builder.AppendLine(Directory.Exists(entry) ? $"- {relative}/" : $"- {relative}");
        }
        return builder.ToString().TrimEnd();
    }

    public string OpenFile(string filePath, bool isPreview = false)
    {
        string path = ResolvePath(filePath);
        var maxLines = isPreview ? 120 : 250;
        return $"Opened {GetDisplayPath(path)} (preview={isPreview.ToString().ToLowerInvariant()})\n\n{ReadFile(filePath, 1, maxLines)}";
    }

    public string ReadFileTool(string filePath, int? offset = null, int? limit = null)
        => ReadFile(filePath, offset ?? 1, limit ?? 250);

    public string ReadFile(string relativePath, int startLine = 1, int maxLines = 250)
    {
        string path = ResolvePath(relativePath);
        string[] lines = File.ReadAllLines(path);
        int safeStartLine = Math.Max(1, startLine);
        int safeMaxLines = Math.Clamp(maxLines, 1, 500);
        IEnumerable<string> selected = lines.Skip(safeStartLine - 1).Take(safeMaxLines);
        StringBuilder builder = new();
        var lineNumber = safeStartLine;
        foreach (string line in selected)
        {
            builder.Append(lineNumber++).Append(": ").AppendLine(line);
        }
        return builder.ToString().TrimEnd();
    }

    public string CreateFile(string filePath, string content)
    {
        string path = ResolvePath(filePath);
        if (File.Exists(path))
        {
            throw new InvalidOperationException($"The file '{GetDisplayPath(path)}' already exists.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return $"Created {GetDisplayPath(path)} with {content.Length} characters.";
    }

    public string WriteFile(string relativePath, string content)
    {
        string path = ResolvePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return $"Wrote {content.Length} characters to {GetDisplayPath(path)}.";
    }

    public string AppendFile(string relativePath, string content)
    {
        string path = ResolvePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, content);
        return $"Appended {content.Length} characters to {GetDisplayPath(path)}.";
    }

    public string InsertEditIntoFile(string filePath, string searchText, string replacementText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            throw new InvalidOperationException("searchText must be provided for insert_edit_into_file.");
        }

        string path = ResolvePath(filePath);
        string content = File.ReadAllText(path);
        int occurrenceCount = CountOccurrences(content, searchText);
        if (occurrenceCount == 0)
        {
            throw new InvalidOperationException($"The requested text was not found in '{GetDisplayPath(path)}'.");
        }

        if (occurrenceCount > 1)
        {
            throw new InvalidOperationException($"The requested text matched {occurrenceCount} regions in '{GetDisplayPath(path)}'. Provide a more specific snippet.");
        }

        string updated = ReplaceFirstOccurrence(content, searchText, replacementText);
        File.WriteAllText(path, updated);
        return $"Updated 1 region in {GetDisplayPath(path)}.";
    }

    public string ApplyPatch(string filePath, string input, string explanation)
    {
        string normalizedPatch = NormalizePatchEnvelope(filePath, input);
        List<PatchOperation> operations = ParsePatch(normalizedPatch);
        if (operations.Count == 0)
        {
            throw new InvalidOperationException("No patch operations were provided.");
        }

        foreach (PatchOperation operation in operations)
        {
            string resolvedPath = ResolvePath(operation.FilePath);
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

                    Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath)!);
                    File.WriteAllText(resolvedPath, string.Join('\n', operation.AddedLines));
                    break;

                case PatchOperationKind.Update:
                    if (!File.Exists(resolvedPath))
                    {
                        throw new InvalidOperationException($"Cannot update '{GetDisplayPath(resolvedPath)}' because it does not exist.");
                    }

                    string currentContent = File.ReadAllText(resolvedPath);
                    string updatedContent = ApplyHunks(currentContent, operation.Hunks, GetDisplayPath(resolvedPath));
                    File.WriteAllText(resolvedPath, updatedContent);
                    break;

                default:
                    throw new InvalidOperationException($"Patch operation '{operation.Kind}' is not supported.");
            }
        }

        return $"Applied patch to {GetDisplayPath(ResolvePath(filePath))}. Explanation: {explanation}";
    }

    public string FileSearch(string query, int maxResults = 200)
    {
        Regex regex = WildcardToRegex(query);
        List<string> matches = EnumerateWorkspaceFiles()
            .Select(path => Path.GetRelativePath(_workspaceRoot, path))
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
        Regex fileRegex = WildcardToRegex(includePattern);
        Regex searchRegex = new(isRegexp ? query : Regex.Escape(query), RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        List<string> matches = [];
        foreach (string file in EnumerateWorkspaceFiles())
        {
            string relative = Path.GetRelativePath(_workspaceRoot, file);
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

            for (int i = 0; i < lines.Length; i++)
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
        string[] terms = ExtractSearchTerms(query);
        if (terms.Length == 0)
        {
            return "No semantic search terms were extracted from the query.";
        }

        List<SemanticSnippet> snippets = [];
        foreach (string file in EnumerateWorkspaceFiles())
        {
            string relative = Path.GetRelativePath(_workspaceRoot, file);
            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch
            {
                continue;
            }

            int fileBonus = ScoreText(relative, terms) * 3;
            for (int i = 0; i < lines.Length; i++)
            {
                int lineScore = ScoreText(lines[i], terms);
                if (lineScore == 0 && fileBonus == 0)
                {
                    continue;
                }

                int start = Math.Max(0, i - 2);
                int end = Math.Min(lines.Length - 1, i + 2);
                snippets.Add(new SemanticSnippet(relative, start + 1, end + 1, fileBonus + lineScore, BuildSnippet(lines, start, end)));
            }
        }

        SemanticSnippet[] ranked = snippets
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
            string id = _backgroundTerminalCommandStore.Start(command, _workspaceRoot);
            return $"Started background terminal session.\nId: {id}\nExplanation: {explanation}\nCommand: {command}";
        }

        ShellCommandResult result = await _shellCommandRunner.RunAsync(command, _workspaceRoot, 120, cancellationToken).ConfigureAwait(false);
        return $"Explanation: {explanation}\n{result}";
    }

    public string GetTerminalOutput(string id)
        => _backgroundTerminalCommandStore.GetOutput(id);

    public async Task<string> RunShellAsync(string command, int timeoutSeconds, CancellationToken cancellationToken)
    {
        ShellCommandResult result = await _shellCommandRunner.RunAsync(command, _workspaceRoot, timeoutSeconds, cancellationToken).ConfigureAwait(false);
        return result.ToString();
    }

    public async Task<string> GetErrorsAsync(string[] filePaths, CancellationToken cancellationToken)
    {
        if (filePaths.Length == 0)
        {
            return "No files were supplied.";
        }

        string? buildTarget = FindDotNetBuildTarget();
        if (buildTarget is null)
        {
            return "No .NET solution or project was found for diagnostics.";
        }

        HashSet<string> requestedPaths = filePaths
            .Select(ResolvePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        ShellCommandResult result = await _shellCommandRunner
            .RunAsync($"dotnet build \"{buildTarget}\" --nologo --no-restore", _workspaceRoot, 180, cancellationToken)
            .ConfigureAwait(false);

        string combinedOutput = string.Join(
            Environment.NewLine,
            new[] { result.StandardOutput, result.StandardError }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        List<string> matches = [];
        Regex regex = new(@"^(?<path>.+?)\((?<line>\d+)(,(?<column>\d+))?\): (?<severity>error|warning) (?<code>[^:]+): (?<message>.+)$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
        foreach (Match match in regex.Matches(combinedOutput))
        {
            string diagnosticPath = Path.GetFullPath(match.Groups["path"].Value);
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

        string mappedEcosystem = MapEcosystem(ecosystem);
        OsvBatchQueryRequest request = new(
            dependencies.Select(dependency => ParseDependency(dependency, mappedEcosystem)).ToArray());

        using HttpRequestMessage message = new(HttpMethod.Post, "https://api.osv.dev/v1/querybatch")
        {
            Content = new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), Encoding.UTF8, "application/json"),
        };

        using HttpResponseMessage response = await OsvHttpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        OsvBatchQueryResponse? payload = JsonSerializer.Deserialize<OsvBatchQueryResponse>(content, JsonSerializerOptions.Web);
        if (payload?.Results is null || payload.Results.Length == 0)
        {
            return "No vulnerability data was returned.";
        }

        StringBuilder builder = new();
        for (int i = 0; i < dependencies.Length; i++)
        {
            builder.AppendLine(dependencies[i]);
            OsvBatchResult result = payload.Results.ElementAtOrDefault(i) ?? new OsvBatchResult(null);
            if (result.Vulns is null || result.Vulns.Length == 0)
            {
                builder.AppendLine("- No known vulnerabilities found.");
                continue;
            }

            foreach (OsvVulnerability vulnerability in result.Vulns)
            {
                string fixedVersions = string.Join(", ", vulnerability.Affected?
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
        foreach (AskQuestionDefinition question in questions)
        {
            builder.Append("- ").Append(question.Header).Append(": ").AppendLine(question.Question);
            builder.Append("  multiSelect=").Append(question.MultiSelect.ToString().ToLowerInvariant())
                .Append(", allowFreeformInput=")
                .AppendLine(question.AllowFreeformInput.ToString().ToLowerInvariant());

            if (question.Options is not null)
            {
                foreach (AskQuestionOption option in question.Options)
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

        string[] terms = ExtractSearchTerms(task);
        List<string> fileCandidates = EnumerateWorkspaceFiles()
            .Select(path => Path.GetRelativePath(_workspaceRoot, path))
            .Where(path => terms.Any(term => path.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        string semanticResults = SemanticSearch(task, 5);
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
            foreach (string candidate in fileCandidates)
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
        string candidate = string.IsNullOrWhiteSpace(path)
            ? _workspaceRoot
            : Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(_workspaceRoot, path));

        if (!IsInsideWorkspace(candidate))
        {
            throw new InvalidOperationException("The requested path escapes the configured workspace root.");
        }

        return candidate;
    }

    private bool IsInsideWorkspace(string path)
    {
        string rootWithSeparator = _workspaceRoot.EndsWith(Path.DirectorySeparatorChar)
            ? _workspaceRoot
            : _workspaceRoot + Path.DirectorySeparatorChar;

        return path.Equals(_workspaceRoot, StringComparison.Ordinal)
            || path.StartsWith(rootWithSeparator, StringComparison.Ordinal);
    }

    private string GetDisplayPath(string fullPath)
    {
        string relative = Path.GetRelativePath(_workspaceRoot, fullPath);
        return relative == "." ? "." : relative;
    }

    private IEnumerable<string> EnumerateWorkspaceFiles()
    {
        return Directory.EnumerateFiles(_workspaceRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path) && !BinaryExtensions.Contains(Path.GetExtension(path)));
    }

    private bool IsIgnoredPath(string path)
    {
        string relative = Path.GetRelativePath(_workspaceRoot, path);
        string[] segments = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
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
        int index = content.IndexOf(searchText, StringComparison.Ordinal);
        return content[..index] + replacementText + content[(index + searchText.Length)..];
    }

    private static string NormalizePatchEnvelope(string filePath, string input)
    {
        string normalized = input.Replace("\r\n", "\n");
        if (normalized.Contains("*** Begin Patch", StringComparison.Ordinal))
        {
            return normalized;
        }

        string patchPath = Path.IsPathRooted(filePath) ? filePath : filePath.Replace('\\', '/');
        return $"*** Begin Patch\n*** Update File: {patchPath}\n@@\n{normalized}\n*** End Patch";
    }

    private List<PatchOperation> ParsePatch(string patch)
    {
        string[] lines = patch.Replace("\r\n", "\n").Split('\n');
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
                string operationPath = line[14..].Trim();
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

            string filePathValue = line[17..].Trim();
            index++;
            if (index < lines.Length && lines[index].StartsWith("*** Move to:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("File moves are not supported.");
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

            operations.Add(PatchOperation.Update(filePathValue, hunks));
        }

        return operations;
    }

    private static string ApplyHunks(string content, IReadOnlyList<PatchHunk> hunks, string displayPath)
    {
        bool hadTrailingNewline = content.EndsWith('\n');
        List<string> lines = content.Replace("\r\n", "\n").Split('\n').ToList();
        if (hadTrailingNewline && lines.Count > 0 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        var searchStart = 0;
        foreach (PatchHunk hunk in hunks)
        {
            List<string> before = hunk.Lines.Where(static line => line.Prefix != '+').Select(static line => line.Text).ToList();
            List<string> after = hunk.Lines.Where(static line => line.Prefix != '-').Select(static line => line.Text).ToList();
            if (before.Count == 0)
            {
                throw new InvalidOperationException("Patch hunks must include at least one context or removed line.");
            }

            int index = FindSequence(lines, before, searchStart);
            if (index < 0)
            {
                throw new InvalidOperationException($"Failed to apply patch to '{displayPath}' because the expected context was not found.");
            }

            lines.RemoveRange(index, before.Count);
            lines.InsertRange(index, after);
            searchStart = index;
        }

        string updated = string.Join('\n', lines);
        return hadTrailingNewline ? updated + '\n' : updated;
    }

    private static int FindSequence(List<string> lines, List<string> sequence, int startIndex)
    {
        if (sequence.Count == 0)
        {
            return startIndex;
        }

        for (int i = Math.Max(0, startIndex); i <= lines.Count - sequence.Count; i++)
        {
            var matched = true;
            for (int j = 0; j < sequence.Count; j++)
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
        string? slnx = Directory.EnumerateFiles(_workspaceRoot, "*.slnx", SearchOption.TopDirectoryOnly).OrderBy(static path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        if (slnx is not null)
        {
            return Path.GetRelativePath(_workspaceRoot, slnx);
        }

        string? sln = Directory.EnumerateFiles(_workspaceRoot, "*.sln", SearchOption.TopDirectoryOnly).OrderBy(static path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        if (sln is not null)
        {
            return Path.GetRelativePath(_workspaceRoot, sln);
        }

        string? project = Directory.EnumerateFiles(_workspaceRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        return project is null ? null : Path.GetRelativePath(_workspaceRoot, project);
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
        foreach (string term in terms)
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
        for (int i = start; i <= end; i++)
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
        int separatorIndex = dependency.LastIndexOf('@');
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
        string escaped = Regex.Escape(string.IsNullOrWhiteSpace(pattern) ? "*" : pattern)
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

    private sealed record PatchOperation(PatchOperationKind Kind, string FilePath, IReadOnlyList<string> AddedLines, IReadOnlyList<PatchHunk> Hunks)
    {
        public static PatchOperation Add(string filePath, IReadOnlyList<string> addedLines)
            => new(PatchOperationKind.Add, filePath, addedLines, []);

        public static PatchOperation Update(string filePath, IReadOnlyList<PatchHunk> hunks)
            => new(PatchOperationKind.Update, filePath, [], hunks);
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

