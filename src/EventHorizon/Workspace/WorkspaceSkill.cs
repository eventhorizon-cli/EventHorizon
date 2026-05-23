using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Workspace;

internal sealed class WorkspaceSkill : AgentClassSkill<WorkspaceSkill>
{
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        name: "workspace",
        description: "Inspect and edit the current coding workspace.");

    protected override string Instructions =>
        "Use this skill whenever you need to inspect files, search code, edit content, or run shell commands inside the configured workspace.";

    [AgentSkillResource("workspace-map")]
    [Description("High-level description of the configured workspace root and its top-level entries.")]
    private static string GetWorkspaceMap(IServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<WorkspaceService>().DescribeWorkspace();
}

