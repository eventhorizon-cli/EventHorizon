using EventHorizon.Configuration;
using EventHorizon.Conversations;
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

    public AgentSkillsProvider? Create(AppOptions options, IServiceProvider services, ConversationSessionDocument? sessionDocument = null)
    {
        if (!options.Agent.EnableSkills)
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
            builder.UseFileSkills(skillDirectories);
        }

        var loggerFactory = services.GetService<ILoggerFactory>();
        if (loggerFactory is not null)
        {
            builder.UseLoggerFactory(loggerFactory);
        }

        return builder.Build();
    }

    private IEnumerable<string> GetSkillDirectories(ConversationSessionDocument? sessionDocument)
    {
        var skillsOptions = _skillsOptionsMonitor.CurrentValue;

        if (!string.IsNullOrWhiteSpace(skillsOptions.StoragePath))
        {
            yield return Path.GetFullPath(skillsOptions.StoragePath);
        }

        if (!string.IsNullOrWhiteSpace(sessionDocument?.SessionSkills.StoragePath))
        {
            yield return Path.GetFullPath(sessionDocument.SessionSkills.StoragePath);
        }
    }
}
