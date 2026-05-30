using System.Text.RegularExpressions;
using EventHorizon.AGUI.DTOs;
using EventHorizon.Conversations;
using EventHorizon.Providers;
using EventHorizon.Workspace;
using Microsoft.Extensions.Options;

namespace EventHorizon.Configuration;

internal sealed class SkillService : ISkillService
{
    private static readonly Regex DangerousScriptPattern = new("(rm\\s+-rf|curl\\s+.+\\|\\s*sh|powershell\\s+-enc)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly AppOptions _options;
    private readonly IUserConfigurationFileService _userConfigurationFileService;
    private readonly IConversationSessionStore _conversationSessionStore;
    private readonly IConversationAgentManager _conversationAgentManager;
    private readonly WorkspaceContext _workspaceContext;

    public SkillService(
        IOptions<AppOptions> options,
        IUserConfigurationFileService userConfigurationFileService,
        IConversationSessionStore conversationSessionStore,
        IConversationAgentManager conversationAgentManager,
        WorkspaceContext workspaceContext)
    {
        _options = options.Value;
        _userConfigurationFileService = userConfigurationFileService;
        _conversationSessionStore = conversationSessionStore;
        _conversationAgentManager = conversationAgentManager;
        _workspaceContext = workspaceContext;
    }

    public async Task<SkillImportResponseDTO> ImportAsync(ImportSkillRequestDTO request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = Validate(request.Path);
        if (errors.Count > 0)
        {
            return new SkillImportResponseDTO
            {
                Success = false,
                Message = "Skill validation failed.",
                Errors = errors,
            };
        }

        var sourcePath = Path.GetFullPath(request.Path);
        if (string.Equals(request.Target, "session", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                return new SkillImportResponseDTO
                {
                    Success = false,
                    Message = "SessionId is required for session skill imports.",
                    Errors = ["SessionId is required for session skill imports."],
                };
            }

            var document = await _conversationSessionStore.LoadAsync(request.SessionId, cancellationToken).ConfigureAwait(false);
            if (document is null)
            {
                return new SkillImportResponseDTO
                {
                    Success = false,
                    Message = "Conversation session was not found.",
                    Errors = ["Conversation session was not found."],
                };
            }

            document.SessionSkills.StoragePath ??= ResolveSessionSkillsRoot(document.Id);
            Directory.CreateDirectory(document.SessionSkills.StoragePath);
            var skill = ImportToCatalog(sourcePath, document.SessionSkills.StoragePath, document.SessionSkills);
            document.UpdatedAt = DateTimeOffset.UtcNow;
            await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            await _conversationAgentManager.InvalidateAsync(document.Id, cancellationToken).ConfigureAwait(false);

            return new SkillImportResponseDTO
            {
                Success = true,
                Message = "Session skill imported successfully.",
                Skill = skill,
                Errors = Array.Empty<string>(),
            };
        }

        _options.Skills.StoragePath ??= ResolveGlobalSkillsRoot();
        Directory.CreateDirectory(_options.Skills.StoragePath);
        var importedSkill = ImportToCatalog(sourcePath, _options.Skills.StoragePath, _options.Skills);
        _userConfigurationFileService.Save(_options);

        return new SkillImportResponseDTO
        {
            Success = true,
            Message = "Skill imported successfully.",
            Skill = importedSkill,
            Errors = Array.Empty<string>(),
        };
    }

    public Task<SkillRemoveResponseDTO> RemoveGlobalAsync(string skillName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return Task.FromResult(new SkillRemoveResponseDTO
            {
                Success = false,
                Message = "Skill name is required.",
                Errors = ["Skill name is required."],
            });
        }

        var skill = _options.Skills.Imported.FirstOrDefault(item => string.Equals(item.Name, skillName, StringComparison.OrdinalIgnoreCase));
        if (skill is null)
        {
            return Task.FromResult(new SkillRemoveResponseDTO
            {
                Success = false,
                Message = "Global skill was not found.",
                Errors = ["Global skill was not found."],
            });
        }

        DeleteSkillDirectory(skill.Path);
        _options.Skills.Imported.RemoveAll(item => string.Equals(item.Name, skillName, StringComparison.OrdinalIgnoreCase));
        _userConfigurationFileService.Save(_options);

        return Task.FromResult(new SkillRemoveResponseDTO
        {
            Success = true,
            Message = "Global skill removed.",
            Errors = Array.Empty<string>(),
        });
    }

    public async Task<SkillRemoveResponseDTO> RemoveSessionAsync(string sessionId, string skillName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(skillName))
        {
            return new SkillRemoveResponseDTO
            {
                Success = false,
                Message = "SessionId and skill name are required.",
                Errors = ["SessionId and skill name are required."],
            };
        }

        var document = await _conversationSessionStore.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return new SkillRemoveResponseDTO
            {
                Success = false,
                Message = "Conversation session was not found.",
                Errors = ["Conversation session was not found."],
            };
        }

        var skill = document.SessionSkills.Imported.FirstOrDefault(item => string.Equals(item.Name, skillName, StringComparison.OrdinalIgnoreCase));
        if (skill is null)
        {
            return new SkillRemoveResponseDTO
            {
                Success = false,
                Message = "Session skill was not found.",
                Errors = ["Session skill was not found."],
            };
        }

        DeleteSkillDirectory(skill.Path);
        document.SessionSkills.Imported.RemoveAll(item => string.Equals(item.Name, skillName, StringComparison.OrdinalIgnoreCase));
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _conversationSessionStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        await _conversationAgentManager.InvalidateAsync(document.Id, cancellationToken).ConfigureAwait(false);

        return new SkillRemoveResponseDTO
        {
            Success = true,
            Message = "Session skill removed.",
            Errors = Array.Empty<string>(),
        };
    }

    private static ImportedSkillOptions ImportToCatalog(string sourcePath, string targetRoot, SkillCatalogOptions catalog)
    {
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

        catalog.Imported.RemoveAll(item => string.Equals(item.Name, skill.Name, StringComparison.OrdinalIgnoreCase));
        catalog.Imported.Add(skill);
        return skill;
    }

    private string ResolveGlobalSkillsRoot()
        => _options.Skills.StoragePath
           ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eventhorizon", "skills");

    private string ResolveSessionSkillsRoot(string sessionId)
        => Path.Combine(ResolveConversationStorageRoot(), sessionId, "skills");

    private string ResolveConversationStorageRoot()
        => !string.IsNullOrWhiteSpace(_options.Conversation.StoragePath)
            ? Path.GetFullPath(_options.Conversation.StoragePath)
            : Path.Combine(_workspaceContext.WorkspaceRoot, ".eventhorizon", "sessions");

    private static void DeleteSkillDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
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
