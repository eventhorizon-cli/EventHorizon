using EventHorizon.Configuration;
using EventHorizon.Conversations;
using EventHorizon.Workspace;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventHorizon.Providers;

internal sealed class SkillProviderFactory : ISkillProviderFactory
{
    public AgentSkillsProvider? Create(AppOptions options, IServiceProvider services, ConversationSessionDocument? sessionDocument = null)
    {
        if (!options.Agent.EnableSkills)
        {
            return null;
        }

        var builder = new AgentSkillsProviderBuilder()
            .UseSkill(services.GetRequiredService<WorkspaceSkill>());

        var skillDirectories = GetSkillDirectories(options, sessionDocument)
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

    private static IEnumerable<string> GetSkillDirectories(AppOptions options, ConversationSessionDocument? sessionDocument)
    {
        if (!string.IsNullOrWhiteSpace(options.Skills.StoragePath))
        {
            yield return Path.GetFullPath(options.Skills.StoragePath);
        }

        if (!string.IsNullOrWhiteSpace(sessionDocument?.SessionSkills.StoragePath))
        {
            yield return Path.GetFullPath(sessionDocument.SessionSkills.StoragePath);
        }
    }
}
