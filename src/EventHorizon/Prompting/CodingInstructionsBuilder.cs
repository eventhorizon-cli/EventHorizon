using EventHorizon.Configuration;
using EventHorizon.Workspace;
using Microsoft.Extensions.Options;

namespace EventHorizon.Prompting;

public interface ICodingInstructionsBuilder
{
    string Build(Configuration.AppOptions options);
}

public sealed class CodingInstructionsBuilder : ICodingInstructionsBuilder
{
    private readonly WorkspaceContext _workspaceContext;
    private readonly McpOptions _mcpOptions;

    public CodingInstructionsBuilder(WorkspaceContext workspaceContext, IOptions<McpOptions> mcpOptions)
    {
        _workspaceContext = workspaceContext;
        _mcpOptions = mcpOptions.Value;
    }

    public string Build(Configuration.AppOptions options)
    {
        List<string> sections =
        [
            "You are EventHorizon, a software engineering agent designed for general software work across any text-based codebase.",
            "Work like a disciplined coding agent: inspect the workspace first, keep a visible plan, execute small verified changes, and summarize what changed.",
            "Operate like a code workbench agent: keep track of transcript context, workspace focus, recent activity, and session checkpoints as you work.",
            "When editing code, prefer minimal diffs, preserve style, verify the result with tests or builds, and avoid speculative changes.",
            "Always operate inside the configured workspace root.",
            "If you cannot complete a request with confidence, explain the blocker clearly and propose the next best step."
        ];

        sections.Add("A shell execution capability is available. Use it when live command output, builds, tests, generators, or tooling feedback is necessary.");

        if (options.Agent.EnableSkills)
        {
            sections.Add("A workspace skill is available for browsing files, reading content, editing content, searching code, and running shell commands across many project types.");
        }

        if (options.Agent.EnableMcpTools && _mcpOptions.Servers.Count > 0)
        {
            sections.Add("Additional MCP tools are connected. Use them when they provide specialized capabilities beyond local workspace operations.");
        }

        sections.Add($"The workspace root is: {_workspaceContext.WorkspaceRoot}");

        if (options.Agent.AdditionalSystemPrompts.Length > 0)
        {
            sections.AddRange(options.Agent.AdditionalSystemPrompts);
        }

        return string.Join("\n\n", sections);
    }
}
