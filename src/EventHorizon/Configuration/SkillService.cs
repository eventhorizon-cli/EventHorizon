using System.Text.RegularExpressions;

namespace EventHorizon.Configuration;

internal sealed class SkillService : ISkillService
{
    private static readonly Regex DangerousScriptPattern = new("(rm\\s+-rf|curl\\s+.+\\|\\s*sh|powershell\\s+-enc)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly AppOptions _options;
    private readonly IUserConfigurationFileService _userConfigurationFileService;

    public SkillService(AppOptions options, IUserConfigurationFileService userConfigurationFileService)
    {
        _options = options;
        _userConfigurationFileService = userConfigurationFileService;
    }

    public Task<SkillImportResponse> ImportAsync(ImportSkillRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = Validate(request.Path);
        if (errors.Count > 0)
        {
            return Task.FromResult(new SkillImportResponse
            {
                Success = false,
                Message = "Skill validation failed.",
                Errors = errors,
            });
        }

        var sourcePath = Path.GetFullPath(request.Path);
        var targetRoot = _options.Skills.StoragePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eventhorizon", "skills");
        Directory.CreateDirectory(targetRoot);
        var skillName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var targetPath = Path.Combine(targetRoot, skillName);
        if (Directory.Exists(targetPath))
        {
            Directory.Delete(targetPath, recursive: true);
        }

        CopyDirectory(sourcePath, targetPath);
        var skill = new ImportedSkillOptions
        {
            Name = skillName,
            Path = targetPath,
            Description = ExtractDescription(Path.Combine(targetPath, "SKILL.md")),
            ImportedAt = DateTimeOffset.UtcNow,
        };
        _options.Skills.StoragePath = targetRoot;
        _options.Skills.Imported.RemoveAll(item => string.Equals(item.Name, skill.Name, StringComparison.OrdinalIgnoreCase));
        _options.Skills.Imported.Add(skill);
        _userConfigurationFileService.Save(_options);

        return Task.FromResult(new SkillImportResponse
        {
            Success = true,
            Message = "Skill imported successfully.",
            Skill = skill,
            Errors = Array.Empty<string>(),
        });
    }

    private static List<string> Validate(string path)
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add("Skill path is required.");
            return errors;
        }

        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
        {
            errors.Add("Skill folder does not exist.");
            return errors;
        }

        var skillFile = Path.Combine(fullPath, "SKILL.md");
        if (!File.Exists(skillFile))
        {
            errors.Add("SKILL.md is required.");
            return errors;
        }

        var content = File.ReadAllText(skillFile);
        if (string.IsNullOrWhiteSpace(content))
        {
            errors.Add("SKILL.md cannot be empty.");
        }

        if (!content.Contains("# ", StringComparison.Ordinal))
        {
            errors.Add("SKILL.md must contain a title heading.");
        }

        if (DangerousScriptPattern.IsMatch(content))
        {
            errors.Add("Skill contains potentially dangerous script content.");
        }

        return errors;
    }

    private static string ExtractDescription(string skillFile)
    {
        var lines = File.ReadAllLines(skillFile)
            .Select(static line => line.Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .ToArray();
        return lines.FirstOrDefault() ?? "Imported skill";
    }

    private static void CopyDirectory(string sourcePath, string targetPath)
    {
        Directory.CreateDirectory(targetPath);
        foreach (var file in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, file);
            var destination = Path.Combine(targetPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }
}

