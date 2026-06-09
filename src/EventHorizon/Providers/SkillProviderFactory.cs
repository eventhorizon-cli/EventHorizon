using EventHorizon.Configuration;
using EventHorizon.Engine.Sessions;
using EventHorizon.Workspace;
using EventHorizon.Workspace.Skills;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHorizon.Providers;

internal sealed class SkillProviderFactory : ISkillProviderFactory
{
    private readonly IOptionsMonitor<SkillsOptions> _skillsOptionsMonitor;
    private readonly ISessionStore _sessionStore;

    public SkillProviderFactory(IOptionsMonitor<SkillsOptions> skillsOptionsMonitor, ISessionStore sessionStore)
    {
        _skillsOptionsMonitor = skillsOptionsMonitor;
        _sessionStore = sessionStore;
    }

    public AgentSkillsProvider? Create(AgentOptions options, IServiceProvider services,
        SessionDocument? sessionDocument = null)
    {
        if (!options.EnableSkills)
        {
            return null;
        }

        var builder = new AgentSkillsProviderBuilder()
            .UseSkills(
                new WorkspaceSkill(),
                new DotNetFileRunnerSkill());

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

        var workspace = string.IsNullOrWhiteSpace(sessionDocument.WorkspaceId)
            ? null
            : _sessionStore.LoadWorkspaceAsync(sessionDocument.WorkspaceId, CancellationToken.None).GetAwaiter().GetResult();

        if (workspace is null)
        {
            yield break;
        }

        foreach (var path in GetEnabledSkillDirectories(workspace.WorkspaceSkills))
        {
            yield return path;
        }
    }

    private static IEnumerable<string> GetEnabledSkillDirectories(SkillsOptions options)
        => options.Imported
            .Where(static skill => skill.Enabled && !string.IsNullOrWhiteSpace(skill.Path))
            .Select(static skill => Path.GetFullPath(skill.Path));
}
