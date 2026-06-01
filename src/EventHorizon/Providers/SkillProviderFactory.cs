using EventHorizon.Configuration;
using EventHorizon.Engine.Sessions;
using EventHorizon.Workspace;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHorizon.Providers;

internal sealed class SkillProviderFactory : ISkillProviderFactory
{
    private readonly IOptionsMonitor<SkillsOptions> _skillsOptionsMonitor;

    public SkillProviderFactory(IOptionsMonitor<SkillsOptions> skillsOptionsMonitor)
    {
        _skillsOptionsMonitor = skillsOptionsMonitor;
    }

    public AgentSkillsProvider? Create(AgentOptions options, IServiceProvider services,
        SessionDocument? sessionDocument = null)
    {
        if (!options.EnableSkills)
        {
            return null;
        }

        var builder = new AgentSkillsProviderBuilder()
            .UseSkill(services.GetRequiredService<WorkspaceSkill>());

        var skillDirectories = GetSkillDirectories(sessionDocument)
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (skillDirectories.Length > 0)
        {
            builder.UseFileSkills(skillDirectories, scriptRunner: SubprocessScriptRunner.RunAsync);
        }

        var loggerFactory = services.GetService<ILoggerFactory>();
        if (loggerFactory is not null)
        {
            builder.UseLoggerFactory(loggerFactory);
        }

        return builder.Build();
    }

    private IEnumerable<string> GetSkillDirectories(SessionDocument? sessionDocument)
    {
        var skillsOptions = _skillsOptionsMonitor.CurrentValue;

        foreach (var path in GetEnabledSkillDirectories(skillsOptions))
        {
            yield return path;
        }

        if (sessionDocument is null)
        {
            yield break;
        }

        foreach (var path in GetEnabledSkillDirectories(sessionDocument.SessionSkills))
        {
            yield return path;
        }
    }

    private static IEnumerable<string> GetEnabledSkillDirectories(SkillsOptions options)
        => options.Imported
            .Where(static skill => skill.Enabled && !string.IsNullOrWhiteSpace(skill.Path))
            .Select(static skill => Path.GetFullPath(skill.Path));
}
